using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Bomb;

public sealed record BombExplodedEvent(
    BombSite Site,
    string MapName,
    int RoundNumber,
    DateTimeOffset OccurredAt
) : IGameEvent;
