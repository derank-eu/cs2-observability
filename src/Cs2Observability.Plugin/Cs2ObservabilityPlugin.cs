using Cs2Observability.Core.Exporters;
using Cs2Observability.Plugin.Configuration;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace Cs2Observability.Plugin;

/// <summary>
/// CounterStrikeSharp plugin entry point.
/// Hooks CS2 game events and dispatches them to all registered <see cref="IGameEventExporter"/> instances.
/// Configuration is loaded from:
///   addons/counterstrikesharp/configs/plugins/Cs2Observability/Cs2Observability.json
/// </summary>
public sealed class Cs2ObservabilityPlugin : BasePlugin, IPluginConfig<ObservabilityConfig>
{
    public override string ModuleName => "CS2 Observability";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "";
    public override string ModuleDescription => "Dispatches structured game events to observability exporters.";

    public ObservabilityConfig Config { get; set; } = new();

    private readonly IReadOnlyList<IGameEventExporter> _exporters;

    public Cs2ObservabilityPlugin(IEnumerable<IGameEventExporter> exporters)
    {
        _exporters = exporters.ToList();
    }

    public void OnConfigParsed(ObservabilityConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandlers();

        AddCommand("css_observability_reload", "Reload observability config and reconnect exporters",
            (_, _) =>
            {
                // CSS will call OnConfigParsed automatically when re-parsing;
                // exporters that hold connections should re-read Config after this point.
                Server.PrintToConsole("[CS2 Observability] Config reloaded.");
            });

        // TODO: dispatch PluginStartupEvent through _exporters
    }

    public override void Unload(bool hotReload)
    {
        // TODO: dispatch PluginShutdownEvent through _exporters
    }

    private void RegisterEventHandlers()
    {
        // TODO: register per-category handlers guarded by Config.Events.*
        // Example structure:
        //   if (Config.Events.Player) { RegisterEventHandler<EventPlayerConnectFull>(...); }
        //   if (Config.Events.Kills)  { RegisterEventHandler<EventPlayerDeath>(...); }
        //   if (Config.Events.Rounds) { RegisterEventHandler<EventRoundStart>(...); ... }
        //   if (Config.Events.Bomb)   { RegisterEventHandler<EventBombPlanted>(...); ... }
        //   if (Config.Events.Chat)   { RegisterEventHandler<EventPlayerChat>(...); }
    }
}
