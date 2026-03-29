using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Server;

public sealed record MapChangeEvent(
    string FromMap,
    string ToMap,
    /// <summary>E.g. "vote", "admin", "mapcycle".</summary>
    string Reason,
    DateTimeOffset OccurredAt
) : IGameEvent;
