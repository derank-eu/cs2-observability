using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Bomb;

public sealed record BombDefuseAbortEvent(
    PlayerInfo Player,
    string MapName,
    int RoundNumber,
    DateTimeOffset OccurredAt
) : IGameEvent;
