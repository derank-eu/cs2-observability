using Cs2Observability.Core.Events.Economy;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Cs2Observability.Plugin;

public sealed partial class Cs2ObservabilityPlugin
{
    private void RegisterEconomyHandlers()
    {
        RegisterEventHandler<EventItemPurchase>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            var moneyRemaining = player.InGameMoneyServices?.Account ?? 0;

            Dispatch(new PlayerBuyEvent(
                Player:          ToPlayerInfo(player),
                Item:            e.Weapon,
                Cost:            0,          // Engine does not expose purchase cost in this event
                MoneyRemaining:  moneyRemaining,
                MapName:         Server.MapName,
                RoundNumber:     _currentRound,
                OccurredAt:      DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });
    }
}
