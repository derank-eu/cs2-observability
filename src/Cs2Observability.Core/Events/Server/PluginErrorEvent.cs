using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Server;

public sealed record PluginErrorEvent(
    string ExceptionType,
    string Message,
    string? StackTrace,
    /// <summary>Human-readable description of what the plugin was doing when the error occurred.</summary>
    string? Context,
    DateTimeOffset OccurredAt
) : IGameEvent;
