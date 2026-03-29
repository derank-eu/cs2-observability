using Cs2Observability.Core.Events.Game;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Cs2Observability.Plugin;

public sealed partial class Cs2ObservabilityPlugin
{
    private void RegisterKillHandlers()
    {
        RegisterEventHandler<EventPlayerDeath>((e, _) =>
        {
            var victim   = e.Userid;
            var attacker = e.Attacker;

            if (victim?.IsValid != true) return HookResult.Continue;

            var isSuicide  = attacker?.IsValid != true || attacker.SteamID == victim.SteamID;
            var isTeamKill = !isSuicide && attacker!.TeamNum == victim.TeamNum;

            Dispatch(new KillEvent(
                Attacker:         isSuicide ? ToPlayerInfo(victim) : ToPlayerInfo(attacker!),
                Victim:           ToPlayerInfo(victim),
                Weapon:           e.Weapon,
                IsHeadshot:       e.Headshot,
                IsPenetration:    e.Penetrated > 0,
                IsNoscope:        e.Noscope,
                IsThroughSmoke:   e.Thrusmoke,
                IsAttackerBlind:  e.Attackerblind,
                DistanceUnits:    e.Distance,
                IsTeamKill:       isTeamKill,
                IsSuicide:        isSuicide,
                MapName:          Server.MapName,
                RoundNumber:      _currentRound,
                OccurredAt:       DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });
    }
}
