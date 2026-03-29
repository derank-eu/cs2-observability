using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Bomb;

public sealed record BombDefusedEvent(
    PlayerInfo Player,
    BombSite Site,
    string MapName,
    int RoundNumber,
    TimeSpan TimeRemainingOnBomb,
    DateTimeOffset OccurredAt
) : IGameEvent;
