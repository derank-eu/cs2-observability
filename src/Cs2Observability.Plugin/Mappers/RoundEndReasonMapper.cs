using Cs2Observability.Core.Enums;

namespace Cs2Observability.Plugin.Mappers;

internal static class RoundEndReasonMapper
{
    /// <summary>
    /// Maps the engine's round end reason integer to our domain enum.
    /// Values sourced from CS2 game_shared.h / CSRoundEndReason.
    /// </summary>
    internal static RoundEndReason Map(int csReason) => csReason switch
    {
        1  => RoundEndReason.TargetBombed,
        7  => RoundEndReason.BombDefused,
        8  => RoundEndReason.TEliminated,         // CT win — all Ts eliminated
        9  => RoundEndReason.CTEliminated,         // T win — all CTs eliminated
        10 => RoundEndReason.Draw,
        11 => RoundEndReason.HostagesRescued,
        12 => RoundEndReason.TargetSaved,
        13 => RoundEndReason.HostagesKilled,
        16 => RoundEndReason.GameStart,
        17 => RoundEndReason.TerroristsSurrender,
        18 => RoundEndReason.CTSurrender,
        _  => RoundEndReason.Unknown,
    };
}
