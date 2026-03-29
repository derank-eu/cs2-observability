using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Economy;

public sealed record PlayerBuyEvent(
    PlayerInfo Player,
    string Item,
    int Cost,
    int MoneyRemaining,
    string MapName,
    int RoundNumber,
    DateTimeOffset OccurredAt
) : IGameEvent;
