using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Bomb;

public sealed record BombPlantedEvent(
    PlayerInfo Player,
    BombSite Site,
    string MapName,
    int RoundNumber,
    TimeSpan TimeElapsedInRound,
    DateTimeOffset OccurredAt
) : IGameEvent;
