using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Components;
using Content.Shared.Ghost;
using Robust.Shared.Physics.Components;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.MobState;
using Content.Server.Storage.Components;
using Content.Server.Visible;
using Content.Server.Warps;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Examine;
using Content.Shared.Follower;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Commands
{
    sealed class RespawnCommand : IConsoleCommand
    {
        public string Command => "respawn";
        public string Description => "Respawns a player, kicking them back to the lobby.";
        public string Help => "respawn [player]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length > 1)
            {
                shell.WriteLine("Must provide <= 1 argument.");
                return;
            }

            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            var ticker = EntitySystem.Get<GameTicker>();

            NetUserId userId;
            if (args.Length == 0)
            {
                if (player == null)
                {
                    shell.WriteLine("If not a player, an argument must be given.");
                    return;
                }

                userId = player.UserId;
            }
            else if (!playerMgr.TryGetUserId(args[0], out userId))
            {
                shell.WriteLine("Unknown player");
                return;
            }

            if (!playerMgr.TryGetSessionById(userId, out var targetPlayer))
            {
                if (!playerMgr.TryGetPlayerData(userId, out var data))
                {
                    shell.WriteLine("Unknown player");
                    return;
                }

                data.ContentData()?.WipeMind();
                shell.WriteLine("Player is not currently online, but they will respawn if they come back online");
                return;
            }

            ticker.Respawn(targetPlayer);
        }
    }

    sealed class RespawnSelfCommand : IConsoleCommand
    {
        public string Command => "respawnme";
        public string Description => "Respawns a player, kicking them back to the lobby.";
        public string Help => "respawnme";

        [Dependency] private readonly IChatManager _chatManager = default!;
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            var ticker = EntitySystem.Get<GameTicker>();

            if (player == null)
            {
                return;
            }

            if (playerMgr.TryGetSessionById(player.UserId, out var targetPlr))
            {
                if (!playerMgr.TryGetPlayerData(player.UserId, out var data))
                {
                    shell.WriteLine("Неопознанный игрок.");
                    return;
                }

                var mind = data.ContentData()?.Mind;
                if (mind == null)
                {
                    _chatManager.DispatchServerMessage(player, "Вы не можете возродиться!");
                    return;
                }
                else
                {
                    ticker.Respawn(player);
                    _chatManager.DispatchServerMessage(player, "Вы вернулись в лобби. Учтите, что использование того же персонажа запрещено!");
                    return;
                }
            }
        }
    }
}
