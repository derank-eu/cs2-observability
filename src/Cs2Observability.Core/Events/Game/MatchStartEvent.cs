using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Game;

public sealed record MatchStartEvent(
    string MapName,
    string GameMode,
    int MaxRounds,
    DateTimeOffset OccurredAt
) : IGameEvent;
