using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Game;

public sealed record MatchEndEvent(
    string MapName,
    GameTeam WinnerTeam,
    int TerroristScore,
    int CounterTerroristScore,
    TimeSpan MatchDuration,
    DateTimeOffset OccurredAt
) : IGameEvent;
