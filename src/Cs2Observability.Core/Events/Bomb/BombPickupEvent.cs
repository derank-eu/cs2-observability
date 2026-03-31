using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Bomb;

public sealed record BombPickupEvent(
    PlayerInfo Player,
    string MapName,
    int RoundNumber,
    DateTimeOffset OccurredAt
) : IGameEvent;
