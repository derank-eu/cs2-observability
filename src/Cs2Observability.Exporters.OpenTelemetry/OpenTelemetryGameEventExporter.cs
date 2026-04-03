using Cs2Observability.Core.Events;
using Cs2Observability.Core.Events.Bomb;
using Cs2Observability.Core.Events.Chat;
using Cs2Observability.Core.Events.Economy;
using Cs2Observability.Core.Events.Game;
using Cs2Observability.Core.Events.Player;
using Cs2Observability.Core.Events.Server;
using Cs2Observability.Core.Configuration;
using Cs2Observability.Core.Exporters;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;

namespace Cs2Observability.Exporters.OpenTelemetry;

/// <summary>
/// Exports game events as structured OpenTelemetry log records via OTLP.
/// Each event type maps to a named log entry with typed attributes that become
/// OTel log record body / attribute fields in the collector.
/// </summary>
public sealed class OpenTelemetryGameEventExporter : IGameEventExporter, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger        _logger;
    private readonly MeterProvider  _meterProvider;
    private readonly Meter          _meter;
    private readonly OtelDiagnosticListener _diagnostics = new();

    /// <summary>
    /// Stamped on every log record as structured attributes so they appear in the log body
    /// regardless of how the collector handles resource attributes.
    /// </summary>
    private readonly IReadOnlyDictionary<string, object> _globalLogAttrs;

    // ---- Metrics instruments -----------------------------------------------
    private readonly Counter<long>     _kills;
    private readonly Counter<long>     _playerConnects;
    private readonly Counter<long>     _playerDisconnects;
    private readonly Counter<long>     _rounds;
    private readonly Histogram<double> _roundDuration;
    private readonly Counter<long>     _bombPlanted;
    private readonly Counter<long>     _bombDefused;
    private readonly Counter<long>     _bombExploded;
    private readonly Counter<long>     _economyBuys;
    private readonly Counter<long>     _economySpend;

    public OpenTelemetryGameEventExporter(OtlpConfig otlp, ServiceConfig service)
    {
        _globalLogAttrs = service.Attributes.ToDictionary(kv => kv.Key, kv => (object)kv.Value);

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(service.Name)
            .AddAttributes(service.Attributes.Select(kv =>
                new KeyValuePair<string, object>(kv.Key, kv.Value)));

        var protocol = otlp.Protocol.Equals("grpc", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;

        // For http/protobuf the .NET OTel SDK uses the Endpoint URI as-is when set
        // explicitly, so we must include the signal-specific path ourselves.
        var baseEndpoint = otlp.Endpoint.TrimEnd('/');
        Uri LogsEndpoint()    => protocol == OtlpExportProtocol.Grpc
            ? new Uri(baseEndpoint)
            : new Uri(baseEndpoint + "/v1/logs");
        Uri MetricsEndpoint() => protocol == OtlpExportProtocol.Grpc
            ? new Uri(baseEndpoint)
            : new Uri(baseEndpoint + "/v1/metrics");

        var headers = otlp.Headers.Count > 0
            ? string.Join(",", otlp.Headers.Select(kv => $"{kv.Key}={kv.Value}"))
            : null;

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddOpenTelemetry(logging =>
            {
                logging.SetResourceBuilder(resourceBuilder);
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes           = true;
                // Simple processor: export is synchronous — any error surfaces immediately
                // instead of being silently dropped by the background batch thread.
                logging.AddOtlpExporter((otelOtlp, processorOptions) =>
                {
                    otelOtlp.Endpoint            = LogsEndpoint();
                    otelOtlp.Protocol            = protocol;
                    otelOtlp.TimeoutMilliseconds = otlp.TimeoutMs;
                    if (headers is not null)
                        otelOtlp.Headers = headers;
                    processorOptions.ExportProcessorType = ExportProcessorType.Batch;
                });
            });
        });

        _meter = new Meter("cs2.events");
        _meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter("cs2.events")
            .AddOtlpExporter(otelOtlp =>
            {
                otelOtlp.Endpoint            = MetricsEndpoint();
                otelOtlp.Protocol            = protocol;
                otelOtlp.TimeoutMilliseconds = otlp.TimeoutMs;
                if (headers is not null)
                    otelOtlp.Headers = headers;
            })
            .Build()!;

        _kills             = _meter.CreateCounter<long>("cs2.kills.total",            description: "Total kill events");
        _playerConnects    = _meter.CreateCounter<long>("cs2.players.connects.total",    description: "Player connection events");
        _playerDisconnects = _meter.CreateCounter<long>("cs2.players.disconnects.total", description: "Player disconnection events");
        _rounds            = _meter.CreateCounter<long>("cs2.rounds.total",            description: "Rounds completed");
        _roundDuration     = _meter.CreateHistogram<double>("cs2.round.duration.seconds", unit: "s", description: "Round duration in seconds");
        _bombPlanted       = _meter.CreateCounter<long>("cs2.bomb.planted.total",      description: "Bomb plant events");
        _bombDefused       = _meter.CreateCounter<long>("cs2.bomb.defused.total",      description: "Bomb defuse events");
        _bombExploded      = _meter.CreateCounter<long>("cs2.bomb.exploded.total",     description: "Bomb explosion events");
        _economyBuys       = _meter.CreateCounter<long>("cs2.economy.buys.total",      description: "Items purchased");
        _economySpend      = _meter.CreateCounter<long>("cs2.economy.spend.total",     unit: "$",   description: "Total money spent on purchases");

        _logger = _loggerFactory.CreateLogger("cs2.events");
    }

    public Task ExportAsync(IGameEvent gameEvent, CancellationToken ct = default)
    {
        Log(gameEvent);
        return Task.CompletedTask;
    }

    private void Log(IGameEvent evt)
    {
        using var scope = _logger.BeginScope(_globalLogAttrs);
        switch (evt)
        {
            // ---- Player ----------------------------------------------------------------------------------------------------------------
            case PlayerConnectEvent e:
                _logger.LogInformation(
                    "player.connect {SteamId} {PlayerName} {IpAddress} {CountryCode} {PingMs}",
                    e.SteamId, e.PlayerName, e.IpAddress, e.CountryCode ?? "unknown", e.PingMs);
                _playerConnects.Add(1);
                break;

            case PlayerDisconnectEvent e:
                _logger.LogInformation(
                    "player.disconnect {SteamId} {PlayerName} {Team} {Reason} {SessionDurationSeconds}",
                    e.Player.SteamId, e.Player.PlayerName, e.Player.Team,
                    e.Reason, (int)e.SessionDuration.TotalSeconds);
                _playerDisconnects.Add(1, new KeyValuePair<string, object?>("reason", e.Reason));
                break;

            case PlayerNameChangeEvent e:
                _logger.LogInformation(
                    "player.name_change {SteamId} {OldName} {NewName}",
                    e.SteamId, e.OldName, e.NewName);
                break;

            case PlayerTeamChangeEvent e:
                _logger.LogInformation(
                    "player.team_change {SteamId} {PlayerName} {FromTeam} {ToTeam}",
                    e.Player.SteamId, e.Player.PlayerName, e.FromTeam, e.ToTeam);
                break;

            case PlayerKickEvent e:
                _logger.LogInformation(
                    "player.kick {SteamId} {PlayerName} {Reason} {KickedBySteamId}",
                    e.Player.SteamId, e.Player.PlayerName, e.Reason,
                    e.KickedBySteamId ?? "server");
                break;

            case PlayerBanEvent e:
                _logger.LogWarning(
                    "player.ban {SteamId} {PlayerName} {Source} {Reason} {DurationSeconds} {BannedBySteamId}",
                    e.SteamId, e.PlayerName, e.Source, e.Reason ?? "unspecified",
                    (int?)e.Duration?.TotalSeconds ?? -1,
                    e.BannedBySteamId ?? "server");
                break;

            case PlayerMuteEvent e:
                _logger.LogInformation(
                    "player.mute {SteamId} {PlayerName} {IsMuted} {MutedBySteamId}",
                    e.Player.SteamId, e.Player.PlayerName, e.IsMuted,
                    e.MutedBySteamId ?? "server");
                break;

            // ---- Kills ------------------------------------------------------------------------------------------------------------------
            case KillEvent e:
                _logger.LogInformation(
                    "kill {AttackerSteamId} {AttackerName} {AttackerTeam} {VictimSteamId} {VictimName} {VictimTeam} " +
                    "{Weapon} {IsHeadshot} {IsPenetration} {IsNoscope} {IsThroughSmoke} " +
                    "{IsAttackerBlind} {DistanceUnits} {IsTeamKill} {IsSuicide} {MapName} {RoundNumber}",
                    e.Attacker.SteamId, e.Attacker.PlayerName, e.Attacker.Team,
                    e.Victim.SteamId,   e.Victim.PlayerName,   e.Victim.Team,
                    e.Weapon, e.IsHeadshot, e.IsPenetration, e.IsNoscope,
                    e.IsThroughSmoke, e.IsAttackerBlind, e.DistanceUnits,
                    e.IsTeamKill, e.IsSuicide, e.MapName, e.RoundNumber);
                _kills.Add(1,
                    new KeyValuePair<string, object?>("weapon",        e.Weapon),
                    new KeyValuePair<string, object?>("is_headshot",   e.IsHeadshot),
                    new KeyValuePair<string, object?>("is_teamkill",   e.IsTeamKill),
                    new KeyValuePair<string, object?>("is_suicide",    e.IsSuicide),
                    new KeyValuePair<string, object?>("map",           e.MapName),
                    new KeyValuePair<string, object?>("attacker_team", e.Attacker.Team.ToString()),
                    new KeyValuePair<string, object?>("victim_team",   e.Victim.Team.ToString()));
                break;

            // ---- Rounds / Match ------------------------------------------------------------------------------------------------
            case MatchStartEvent e:
                _logger.LogInformation(
                    "match.start {MapName} {GameMode} {MaxRounds}",
                    e.MapName, e.GameMode, e.MaxRounds);
                break;

            case RoundStartEvent e:
                _logger.LogInformation(
                    "round.start {RoundNumber} {MapName} {GameMode} {TerroristCount} {CounterTerroristCount}",
                    e.RoundNumber, e.MapName, e.GameMode, e.TerroristCount, e.CounterTerroristCount);
                break;

            case RoundEndEvent e:
                _logger.LogInformation(
                    "round.end {RoundNumber} {MapName} {WinnerTeam} {Reason} " +
                    "{TerroristScore} {CounterTerroristScore} {RoundDurationSeconds}",
                    e.RoundNumber, e.MapName, e.WinnerTeam, e.Reason,
                    e.TerroristScore, e.CounterTerroristScore, (int)e.RoundDuration.TotalSeconds);
                _rounds.Add(1,
                    new KeyValuePair<string, object?>("map",         e.MapName),
                    new KeyValuePair<string, object?>("winner_team", e.WinnerTeam.ToString()),
                    new KeyValuePair<string, object?>("reason",      e.Reason));
                _roundDuration.Record(e.RoundDuration.TotalSeconds,
                    new KeyValuePair<string, object?>("map", e.MapName));
                break;

            case HalftimeEvent e:
                _logger.LogInformation(
                    "match.halftime {MapName} {TerroristScore} {CounterTerroristScore}",
                    e.MapName, e.TerroristScore, e.CounterTerroristScore);
                break;

            case MatchEndEvent e:
                _logger.LogInformation(
                    "match.end {MapName} {WinnerTeam} {TerroristScore} {CounterTerroristScore} {MatchDurationSeconds}",
                    e.MapName, e.WinnerTeam, e.TerroristScore, e.CounterTerroristScore,
                    (int)e.MatchDuration.TotalSeconds);
                break;

            // ---- Bomb --------------------------------------------------------------------------------------------------------------------
            case BombPickupEvent e:
                _logger.LogInformation(
                    "bomb.pickup {SteamId} {PlayerName} {MapName} {RoundNumber}",
                    e.Player.SteamId, e.Player.PlayerName, e.MapName, e.RoundNumber);
                break;

            case BombDroppedEvent e:
                _logger.LogInformation(
                    "bomb.dropped {SteamId} {PlayerName} {MapName} {RoundNumber}",
                    e.Player.SteamId, e.Player.PlayerName, e.MapName, e.RoundNumber);
                break;

            case BombPlantedEvent e:
                _logger.LogInformation(
                    "bomb.planted {SteamId} {PlayerName} {Site} {MapName} {RoundNumber} {TimeElapsedInRoundSeconds}",
                    e.Player.SteamId, e.Player.PlayerName, e.Site, e.MapName,
                    e.RoundNumber, (int)e.TimeElapsedInRound.TotalSeconds);
                _bombPlanted.Add(1,
                    new KeyValuePair<string, object?>("site", e.Site.ToString()),
                    new KeyValuePair<string, object?>("map",  e.MapName));
                break;

            case BombDefuseStartEvent e:
                _logger.LogInformation(
                    "bomb.defuse_start {SteamId} {PlayerName} {HasKit} {MapName} {RoundNumber}",
                    e.Player.SteamId, e.Player.PlayerName, e.HasKit, e.MapName, e.RoundNumber);
                break;

            case BombDefuseAbortEvent e:
                _logger.LogInformation(
                    "bomb.defuse_abort {SteamId} {PlayerName} {MapName} {RoundNumber}",
                    e.Player.SteamId, e.Player.PlayerName, e.MapName, e.RoundNumber);
                break;

            case BombDefusedEvent e:
                _logger.LogInformation(
                    "bomb.defused {SteamId} {PlayerName} {Site} {MapName} {RoundNumber} {TimeRemainingOnBombSeconds}",
                    e.Player.SteamId, e.Player.PlayerName, e.Site, e.MapName,
                    e.RoundNumber, (int)e.TimeRemainingOnBomb.TotalSeconds);
                _bombDefused.Add(1,
                    new KeyValuePair<string, object?>("site", e.Site.ToString()),
                    new KeyValuePair<string, object?>("map",  e.MapName));
                break;

            case BombExplodedEvent e:
                _logger.LogInformation(
                    "bomb.exploded {Site} {MapName} {RoundNumber}",
                    e.Site, e.MapName, e.RoundNumber);
                _bombExploded.Add(1,
                    new KeyValuePair<string, object?>("site", e.Site.ToString()),
                    new KeyValuePair<string, object?>("map",  e.MapName));
                break;

            // ---- Economy --------------------------------------------------------------------------------------------------------------
            case PlayerBuyEvent e:
                _logger.LogInformation(
                    "economy.buy {SteamId} {PlayerName} {Team} {Item} {Cost} {MoneyRemaining} {MapName} {RoundNumber}",
                    e.Player.SteamId, e.Player.PlayerName, e.Player.Team,
                    e.Item, e.Cost, e.MoneyRemaining, e.MapName, e.RoundNumber);
                _economyBuys.Add(1,
                    new KeyValuePair<string, object?>("team", e.Player.Team.ToString()),
                    new KeyValuePair<string, object?>("item", e.Item));
                _economySpend.Add(e.Cost,
                    new KeyValuePair<string, object?>("team", e.Player.Team.ToString()),
                    new KeyValuePair<string, object?>("item", e.Item));
                break;

            case RoundEconomySnapshotEvent e:
                _logger.LogInformation(
                    "economy.round_snapshot {MapName} {RoundNumber} " +
                    "{TAvgMoney} {TEquipValue} {TPlayerCount} " +
                    "{CtAvgMoney} {CtEquipValue} {CtPlayerCount}",
                    e.MapName, e.RoundNumber,
                    e.Terrorists.AverageMoneyAmount,
                    e.Terrorists.TotalEquipmentValue,
                    e.Terrorists.PlayerCount,
                    e.CounterTerrorists.AverageMoneyAmount,
                    e.CounterTerrorists.TotalEquipmentValue,
                    e.CounterTerrorists.PlayerCount);
                break;

            // ---- Chat --------------------------------------------------------------------------------------------------------------------
            case PlayerChatEvent e:
                _logger.LogInformation(
                    "chat.message {SteamId} {PlayerName} {Team} {Channel} {Message} {MapName} {RoundNumber}",
                    e.Player.SteamId, e.Player.PlayerName, e.Player.Team,
                    e.Channel, e.Message, e.MapName, e.RoundNumber);
                break;

            // ---- Server ----------------------------------------------------------------------------------------------------------------
            case MapChangeEvent e:
                _logger.LogInformation(
                    "server.map_change {FromMap} {ToMap} {Reason}",
                    e.FromMap, e.ToMap, e.Reason);
                break;

            case RconCommandEvent e:
                _logger.LogInformation(
                    "server.rcon_command {SourceIp} {Command}",
                    e.SourceIp, e.Command);
                break;

            case ServerConfigChangeEvent e:
                _logger.LogInformation(
                    "server.config_change {CvarName} {OldValue} {NewValue}",
                    e.CvarName, e.OldValue, e.NewValue);
                break;

            case PluginStartupEvent e:
                _logger.LogInformation(
                    "plugin.startup {PluginVersion} {GameVersion}",
                    e.PluginVersion, e.GameVersion);
                break;

            case PluginShutdownEvent e:
                _logger.LogInformation(
                    "plugin.shutdown {Reason}",
                    e.Reason);
                break;

            case PluginErrorEvent e:
                _logger.LogError(
                    "plugin.error {ExceptionType} {Message} {StackTrace} {Context}",
                    e.ExceptionType, e.Message, e.StackTrace ?? "", e.Context ?? "");
                break;
        }
    }

    public void Dispose()
    {
        _meterProvider.Dispose();
        _meter.Dispose();
        _loggerFactory.Dispose();
        _diagnostics.Dispose();
    }

    /// <summary>
    /// Listens to the OpenTelemetry SDK's internal EventSource and writes any
    /// warning/error events to the CS2 server console so they are visible in logs.
    /// </summary>
    private sealed class OtelDiagnosticListener : EventListener
    {
        private static readonly string[] SourcePrefixes = ["OpenTelemetry"];
        public event Action<string>? OnError;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (SourcePrefixes.Any(p => eventSource.Name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                EnableEvents(eventSource, EventLevel.Warning);
        }

        protected override void OnEventWritten(EventWrittenEventArgs e)
        {
            var msg = $"[CS2 Observability] OTel SDK [{e.EventSource.Name}/{e.EventName}]: " +
                      string.Join(" | ", e.Payload is { } p ? p : Enumerable.Empty<object?>());
            OnError?.Invoke(msg);
        }
    }

    /// <summary>Wire up the OTel diagnostic listener to print errors to the server console.</summary>
    public void AttachConsoleErrorSink(Action<string> printToConsole)
        => _diagnostics.OnError += printToConsole;
}
