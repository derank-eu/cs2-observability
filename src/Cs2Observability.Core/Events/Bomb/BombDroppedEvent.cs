using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Bomb;

public sealed record BombDroppedEvent(
    PlayerInfo Player,
    string MapName,
    int RoundNumber,
    DateTimeOffset OccurredAt
) : IGameEvent;
