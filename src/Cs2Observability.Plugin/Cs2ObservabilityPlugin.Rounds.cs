using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events.Economy;
using Cs2Observability.Core.Events.Game;
using Cs2Observability.Core.Shared;
using Cs2Observability.Plugin.Mappers;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Cs2Observability.Plugin;

public sealed partial class Cs2ObservabilityPlugin
{
    private void RegisterRoundHandlers()
    {
        RegisterEventHandler<EventRoundAnnounceMatchStart>((e, _) =>
        {
            _matchStartedAt = DateTimeOffset.UtcNow;
            _currentRound   = 0;

            Dispatch(new MatchStartEvent(
                MapName:    Server.MapName,
                GameMode:   GetGameMode(),
                MaxRounds:  GetMaxRounds(),
                OccurredAt: _matchStartedAt));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventRoundStart>((e, _) =>
        {
            _currentRound++;
            _roundStartedAt = DateTimeOffset.UtcNow;

            var players     = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot).ToList();
            var tCount      = players.Count(p => p.TeamNum == (byte)CsTeam.Terrorist);
            var ctCount     = players.Count(p => p.TeamNum == (byte)CsTeam.CounterTerrorist);

            Dispatch(new RoundStartEvent(
                RoundNumber:          _currentRound,
                MapName:              Server.MapName,
                GameMode:             GetGameMode(),
                TerroristCount:       tCount,
                CounterTerroristCount: ctCount,
                OccurredAt:           _roundStartedAt));

            if (Config.Events.Economy)
                DispatchEconomySnapshot();

            return HookResult.Continue;
        });

        RegisterEventHandler<EventRoundEnd>((e, _) =>
        {
            var duration = DateTimeOffset.UtcNow - _roundStartedAt;

            var teams = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager").ToList();
            _lastTScore  = teams.FirstOrDefault(t => t.TeamNum == (byte)CsTeam.Terrorist)?.Score ?? 0;
            _lastCtScore = teams.FirstOrDefault(t => t.TeamNum == (byte)CsTeam.CounterTerrorist)?.Score ?? 0;
            _lastWinner  = (GameTeam)e.Winner;

            Dispatch(new RoundEndEvent(
                RoundNumber:          _currentRound,
                MapName:              Server.MapName,
                WinnerTeam:           _lastWinner,
                Reason:               RoundEndReasonMapper.Map(e.Reason),
                TerroristScore:       _lastTScore,
                CounterTerroristScore: _lastCtScore,
                RoundDuration:        duration,
                OccurredAt:           DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventCsIntermission>((e, _) =>
        {
            Dispatch(new HalftimeEvent(
                MapName:              Server.MapName,
                TerroristScore:       _lastTScore,
                CounterTerroristScore: _lastCtScore,
                OccurredAt:           DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });

        RegisterEventHandler<EventCsWinPanelMatch>((e, _) =>
        {
            Dispatch(new MatchEndEvent(
                MapName:              Server.MapName,
                WinnerTeam:           _lastWinner,
                TerroristScore:       _lastTScore,
                CounterTerroristScore: _lastCtScore,
                MatchDuration:        DateTimeOffset.UtcNow - _matchStartedAt,
                OccurredAt:           DateTimeOffset.UtcNow));

            return HookResult.Continue;
        });
    }

    private void DispatchEconomySnapshot()
    {
        var players = Utilities.GetPlayers()
            .Where(p => p.IsValid && !p.IsBot && p.InGameMoneyServices is not null)
            .ToList();

        static TeamEconomyInfo BuildTeamInfo(IEnumerable<CCSPlayerController> players, GameTeam team)
        {
            var teamPlayers = players.Where(p => (GameTeam)p.TeamNum == team).ToList();
            if (teamPlayers.Count == 0)
                return new TeamEconomyInfo(team, 0, 0, 0);

            var totalMoney  = teamPlayers.Sum(p => p.InGameMoneyServices!.Account);
            var totalEquip  = teamPlayers.Sum(p => (int)p.PawnArmor);

            return new TeamEconomyInfo(
                Team:               team,
                AverageMoneyAmount: totalMoney / teamPlayers.Count,
                TotalEquipmentValue: totalEquip,
                PlayerCount:        teamPlayers.Count);
        }

        Dispatch(new RoundEconomySnapshotEvent(
            MapName:           Server.MapName,
            RoundNumber:       _currentRound,
            Terrorists:        BuildTeamInfo(players, GameTeam.Terrorist),
            CounterTerrorists: BuildTeamInfo(players, GameTeam.CounterTerrorist),
            OccurredAt:        DateTimeOffset.UtcNow));
    }

    private static int GetMaxRounds() =>
        CounterStrikeSharp.API.Modules.Cvars.ConVar.Find("mp_maxrounds")?.GetPrimitiveValue<int>() ?? 24;
}
