using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Server;

public sealed record ServerConfigChangeEvent(
    string CvarName,
    string OldValue,
    string NewValue,
    DateTimeOffset OccurredAt
) : IGameEvent;
