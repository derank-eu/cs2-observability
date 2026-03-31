using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events.Chat;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace Cs2Observability.Plugin;

public sealed partial class Cs2ObservabilityPlugin
{
    private void RegisterChatHandlers()
    {
        RegisterEventHandler<EventPlayerChat>((e, _) =>
        {
            var player = e.Userid is > 0
                ? Utilities.GetPlayerFromUserid(e.Userid)
                : null;

            if (player?.IsValid != true) return HookResult.Continue;

            Dispatch(new PlayerChatEvent(
                Player:      ToPlayerInfo(player),
                Message:     e.Text,
                Channel:     e.Teamonly ? ChatChannel.Team : ChatChannel.All,
                MapName:     Server.MapName,
                RoundNumber: _currentRound,
                OccurredAt:  DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });
    }
}
