using System.Diagnostics;
using Cs2Observability.Core.Events;
using Cs2Observability.Core.Exporters;

namespace Cs2Observability.Exporters.OpenTelemetry;

/// <summary>
/// Exports game events as OpenTelemetry logs via OTLP.
/// Each <see cref="IGameEvent"/> is translated to a structured log record
/// and emitted through the configured <see cref="ActivitySource"/>.
/// </summary>
public sealed class OpenTelemetryGameEventExporter : IGameEventExporter
{
    // TODO: inject ILogger<OpenTelemetryGameEventExporter> and ActivitySource
    // TODO: map each IGameEvent type to structured OTel log attributes

    public Task ExportAsync(IGameEvent gameEvent, CancellationToken ct = default)
    {
        // TODO: implement per-event mapping
        return Task.CompletedTask;
    }
}
