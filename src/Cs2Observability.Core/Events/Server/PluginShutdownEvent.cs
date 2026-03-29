using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Server;

public sealed record PluginShutdownEvent(
    string Reason,
    DateTimeOffset OccurredAt
) : IGameEvent;
