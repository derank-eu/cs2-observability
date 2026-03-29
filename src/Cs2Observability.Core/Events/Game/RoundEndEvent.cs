using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Game;

public sealed record RoundEndEvent(
    int RoundNumber,
    string MapName,
    GameTeam WinnerTeam,
    RoundEndReason Reason,
    int TerroristScore,
    int CounterTerroristScore,
    TimeSpan RoundDuration,
    DateTimeOffset OccurredAt
) : IGameEvent;
