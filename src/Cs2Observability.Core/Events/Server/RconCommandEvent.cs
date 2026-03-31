using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Server;

/// <summary>
/// Emitted when an RCON command is executed.
/// The caller is responsible for redacting sensitive values (e.g. passwords) before constructing this event.
/// </summary>
public sealed record RconCommandEvent(
    string SourceIp,
    string Command,
    DateTimeOffset OccurredAt
) : IGameEvent;
