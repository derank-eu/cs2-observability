using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Server;

public sealed record PluginStartupEvent(
    string PluginVersion,
    string GameVersion,
    DateTimeOffset OccurredAt
) : IGameEvent;
