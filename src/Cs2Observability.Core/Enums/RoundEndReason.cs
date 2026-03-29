namespace Cs2Observability.Core.Enums;

public enum RoundEndReason
{
    Unknown,
    /// <summary>T win — bomb detonated.</summary>
    TargetBombed,
    /// <summary>CT win — bomb defused.</summary>
    BombDefused,
    /// <summary>T win — all CTs eliminated.</summary>
    CTEliminated,
    /// <summary>CT win — all Ts eliminated.</summary>
    TEliminated,
    /// <summary>CT win — round time expired with no bomb plant.</summary>
    TargetSaved,
    /// <summary>CT win — all hostages rescued.</summary>
    HostagesRescued,
    /// <summary>T win — hostages killed.</summary>
    HostagesKilled,
    TerroristsSurrender,
    CTSurrender,
    Draw,
    GameStart,
}
