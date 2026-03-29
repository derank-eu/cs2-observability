using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events.Bomb;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace Cs2Observability.Plugin;

public sealed partial class Cs2ObservabilityPlugin
{
    private void RegisterBombHandlers()
    {
        RegisterEventHandler<EventBombPickup>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            Dispatch(new BombPickupEvent(
                Player:      ToPlayerInfo(player),
                MapName:     Server.MapName,
                RoundNumber: _currentRound,
                OccurredAt:  DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventBombDropped>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            Dispatch(new BombDroppedEvent(
                Player:      ToPlayerInfo(player),
                MapName:     Server.MapName,
                RoundNumber: _currentRound,
                OccurredAt:  DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventBombPlanted>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            _bombPlantedAt = DateTimeOffset.UtcNow;
            _bombSite      = e.Site == 0 ? BombSite.A : BombSite.B;

            Dispatch(new BombPlantedEvent(
                Player:             ToPlayerInfo(player),
                Site:               _bombSite,
                MapName:            Server.MapName,
                RoundNumber:        _currentRound,
                TimeElapsedInRound: DateTimeOffset.UtcNow - _roundStartedAt,
                OccurredAt:         _bombPlantedAt));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventBombBegindefuse>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            Dispatch(new BombDefuseStartEvent(
                Player:      ToPlayerInfo(player),
                HasKit:      e.Haskit,
                MapName:     Server.MapName,
                RoundNumber: _currentRound,
                OccurredAt:  DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventBombAbortdefuse>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            Dispatch(new BombDefuseAbortEvent(
                Player:      ToPlayerInfo(player),
                MapName:     Server.MapName,
                RoundNumber: _currentRound,
                OccurredAt:  DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventBombDefused>((e, _) =>
        {
            var player = e.Userid;
            if (player?.IsValid != true) return HookResult.Continue;

            var c4Timer         = ConVar.Find("mp_c4timer")?.GetPrimitiveValue<float>() ?? 40f;
            var timeElapsed     = _bombPlantedAt != default
                ? (float)(DateTimeOffset.UtcNow - _bombPlantedAt).TotalSeconds
                : 0f;
            var timeRemaining   = TimeSpan.FromSeconds(Math.Max(0, c4Timer - timeElapsed));

            Dispatch(new BombDefusedEvent(
                Player:             ToPlayerInfo(player),
                Site:               _bombSite,
                MapName:            Server.MapName,
                RoundNumber:        _currentRound,
                TimeRemainingOnBomb: timeRemaining,
                OccurredAt:         DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventBombExploded>((e, _) =>
        {
            Dispatch(new BombExplodedEvent(
                Site:        _bombSite,
                MapName:     Server.MapName,
                RoundNumber: _currentRound,
                OccurredAt:  DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });
    }
}
