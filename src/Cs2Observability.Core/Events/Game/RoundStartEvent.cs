using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Game;

public sealed record RoundStartEvent(
    int RoundNumber,
    string MapName,
    string GameMode,
    int TerroristCount,
    int CounterTerroristCount,
    DateTimeOffset OccurredAt
) : IGameEvent;
