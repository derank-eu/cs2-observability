using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Game;

public sealed record HalftimeEvent(
    string MapName,
    int TerroristScore,
    int CounterTerroristScore,
    DateTimeOffset OccurredAt
) : IGameEvent;
