using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Exporters;

/// <summary>
/// Contract for exporting game events to an external destination.
/// Implementations may target OpenTelemetry, webhooks, message queues, etc.
/// </summary>
public interface IGameEventExporter
{
    Task ExportAsync(IGameEvent gameEvent, CancellationToken ct = default);
}
