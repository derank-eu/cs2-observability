using Cs2Observability.Core.Exporters;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Events;

namespace Cs2Observability.Plugin;

/// <summary>
/// CounterStrikeSharp plugin entry point.
/// Hooks CS2 game events and dispatches them to all registered <see cref="IGameEventExporter"/> instances.
/// </summary>
public sealed class Cs2ObservabilityPlugin : BasePlugin
{
    public override string ModuleName => "CS2 Observability";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "";
    public override string ModuleDescription => "Dispatches structured game events to observability exporters.";

    private readonly IReadOnlyList<IGameEventExporter> _exporters;

    public Cs2ObservabilityPlugin(IEnumerable<IGameEventExporter> exporters)
    {
        _exporters = exporters.ToList();
    }

    public override void Load(bool hotReload)
    {
        // TODO: register game event handlers
        // TODO: dispatch PluginStartupEvent
    }

    public override void Unload(bool hotReload)
    {
        // TODO: dispatch PluginShutdownEvent
    }
}
