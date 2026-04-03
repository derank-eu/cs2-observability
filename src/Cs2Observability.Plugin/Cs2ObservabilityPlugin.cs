using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events.Server;
using Cs2Observability.Core.Exporters;
using Cs2Observability.Core.Shared;
using Cs2Observability.Exporters.OpenTelemetry;
using Cs2Observability.Plugin.Configuration;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;

namespace Cs2Observability.Plugin;

/// <summary>
/// CounterStrikeSharp plugin entry point.
/// Configuration file (auto-generated on first load):
///   addons/counterstrikesharp/configs/plugins/Cs2Observability/Cs2Observability.json
/// Runtime reload:
///   css_observability_reload  (console / RCON)
/// </summary>
public sealed partial class Cs2ObservabilityPlugin : BasePlugin, IPluginConfig<ObservabilityConfig>
{
    public override string ModuleName    => "CS2 Observability";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor  => "";
    public override string ModuleDescription =>
        "Dispatches structured game events to observability exporters via OTLP.";

    public ObservabilityConfig Config { get; set; } = new();

    // ---- Exporters ------------------------------------------------------------------------------------------------------------------------
    private readonly List<IGameEventExporter> _exporters = [];

    // ---- Per-match / per-round state --------------------------------------------------------------------------------------
    internal int             _currentRound   = 0;
    internal DateTimeOffset  _roundStartedAt = DateTimeOffset.UtcNow;
    internal DateTimeOffset  _matchStartedAt = DateTimeOffset.UtcNow;
    /// <summary>Scores as of the last RoundEnd — used by Halftime and MatchEnd where the engine event lacks them.</summary>
    internal int             _lastTScore     = 0;
    internal int             _lastCtScore    = 0;
    internal GameTeam        _lastWinner     = GameTeam.None;

    // ---- Per-player session state --------------------------------------------------------------------------------------------
    /// <summary>SteamID64 → time the player connected (for SessionDuration calculation).</summary>
    internal readonly Dictionary<ulong, DateTimeOffset> _playerConnectTimes = [];

    // ---- Bomb state ------------------------------------------------------------------------------------------------------------------------
    internal DateTimeOffset _bombPlantedAt = default;
    internal BombSite       _bombSite      = BombSite.A;

    // --------------------------------------------------------------------------------------------------------------------------------------------------

    public void OnConfigParsed(ObservabilityConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        _exporters.Clear();

        // Stamp server-level resource attributes derived from the runtime environment.
        // CVar-sourced attributes are only written when non-empty to avoid polluting the resource with blank labels.
        var hostname  = ConVar.Find("hostname")?.StringValue;
        var svTags    = ConVar.Find("sv_tags")?.StringValue;
        var port      = ConVar.Find("hostport")?.GetPrimitiveValue<int>() ?? 0;
        if (!string.IsNullOrEmpty(hostname)) Config.Service.Attributes["server.name"] = hostname;
        if (!string.IsNullOrEmpty(svTags))   Config.Service.Attributes["server.tags"] = svTags;
        Config.Service.Attributes["server.port"] = port.ToString();
        Config.Service.Attributes["host.name"]   = Environment.MachineName;
        Config.Service.Attributes["process.pid"] = Environment.ProcessId.ToString();

        var otelExporter = new OpenTelemetryGameEventExporter(Config.Otlp, Config.Service);
        otelExporter.AttachConsoleErrorSink(Server.PrintToConsole);
        _exporters.Add(otelExporter);

        RegisterEventHandlers();

        AddCommand("css_observability_reload",
            "Reload Cs2Observability config and reconnect exporters",
            (_, _) =>
            {
                foreach (var exporter in _exporters.OfType<IDisposable>())
                    exporter.Dispose();

                _exporters.Clear();
                _exporters.Add(new OpenTelemetryGameEventExporter(Config.Otlp, Config.Service));

                Server.PrintToConsole("[CS2 Observability] Config reloaded.");
            });

        AddCommand("css_observability_envvars",
            "Print all server environment variables (admin only)",
            (player, command) =>
            {
                // null player = server console / RCON — always allowed.
                if (player != null && !AdminManager.PlayerHasPermissions(player, "@css/root"))
                {
                    command.ReplyToCommand("[CS2 Observability] You do not have permission to run this command.");
                    return;
                }

                var vars = Environment.GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .OrderBy(e => (string)e.Key!)
                    .ToList();

                command.ReplyToCommand($"[CS2 Observability] {vars.Count} environment variables:");
                foreach (var entry in vars)
                    command.ReplyToCommand($"  {entry.Key}={entry.Value}");
            });

        Dispatch(new PluginStartupEvent(
            PluginVersion: ModuleVersion,
            GameVersion:   Server.GameDirectory,
            OccurredAt:    DateTimeOffset.UtcNow));
    }

    public override void Unload(bool hotReload)
    {
        Dispatch(new PluginShutdownEvent(
            Reason:     hotReload ? "hot_reload" : "server_shutdown",
            OccurredAt: DateTimeOffset.UtcNow));

        foreach (var exporter in _exporters.OfType<IDisposable>())
            exporter.Dispose();

        _exporters.Clear();
    }

    // ---- Helpers ------------------------------------------------------------------------------------------------------------------------------

    internal void Dispatch(Core.Events.IGameEvent evt)
    {
        foreach (var exporter in _exporters)
        {
            var task = exporter.ExportAsync(evt);
            if (!task.IsCompletedSuccessfully)
                _ = task.ContinueWith(t =>
                    Server.PrintToConsole(
                        $"[CS2 Observability] Export error ({exporter.GetType().Name}): {t.Exception?.GetBaseException().Message}"),
                    TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    internal static PlayerInfo ToPlayerInfo(CCSPlayerController player) => new(
        SteamId:    player.SteamID.ToString(),
        PlayerName: player.PlayerName,
        Team:       (GameTeam)player.TeamNum
    );

    internal static string GetGameMode()
    {
        var gameType = ConVar.Find("game_type")?.GetPrimitiveValue<int>() ?? 0;
        var gameMode = ConVar.Find("game_mode")?.GetPrimitiveValue<int>() ?? 0;
        return (gameType, gameMode) switch
        {
            (0, 0) => "casual",
            (0, 1) => "competitive",
            (0, 2) => "wingman",
            (1, 0) => "arms_race",
            (1, 1) => "flying_scoutsman",
            (1, 2) => "deathmatch",
            (4, 0) => "deathmatch",
            _      => $"type{gameType}_mode{gameMode}",
        };
    }

    private void RegisterEventHandlers()
    {
        if (Config.Events.Player)   RegisterPlayerHandlers();
        if (Config.Events.Kills)    RegisterKillHandlers();
        if (Config.Events.Rounds)   RegisterRoundHandlers();
        if (Config.Events.Bomb)     RegisterBombHandlers();
        if (Config.Events.Economy)  RegisterEconomyHandlers();
        if (Config.Events.Chat)     RegisterChatHandlers();
    }
}
