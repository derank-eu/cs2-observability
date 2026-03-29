using Cs2Observability.Core.Events;
using Cs2Observability.Core.Events.Bomb;
using Cs2Observability.Core.Events.Chat;
using Cs2Observability.Core.Events.Economy;
using Cs2Observability.Core.Events.Game;
using Cs2Observability.Core.Events.Player;
using Cs2Observability.Core.Events.Server;
using Cs2Observability.Core.Exporters;
using Cs2Observability.Plugin.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

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

    public OpenTelemetryGameEventExporter(ObservabilityConfig config)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(config.Service.Name)
            .AddAttributes(config.Service.Attributes.Select(kv =>
                new KeyValuePair<string, object>(kv.Key, kv.Value)));

        var protocol = config.Otlp.Protocol.Equals("grpc", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.Grpc
            : OtlpExportProtocol.HttpProtobuf;

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddOpenTelemetry(logging =>
            {
                logging.SetResourceBuilder(resourceBuilder);
                logging.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint             = new Uri(config.Otlp.Endpoint);
                    otlp.Protocol             = protocol;
                    otlp.TimeoutMilliseconds  = config.Otlp.TimeoutMs;
                    if (config.Otlp.Headers.Count > 0)
                        otlp.Headers = string.Join(",",
                            config.Otlp.Headers.Select(kv => $"{kv.Key}={kv.Value}"));
                });
            });
        });

        _logger = _loggerFactory.CreateLogger("cs2.events");
    }

    public Task ExportAsync(IGameEvent gameEvent, CancellationToken ct = default)
    {
        Log(gameEvent);
        return Task.CompletedTask;
    }

    private void Log(IGameEvent evt)
    {
        switch (evt)
        {
            // ── Player ────────────────────────────────────────────────────────
            case PlayerConnectEvent e:
                _logger.LogInformation(
                    "player.connect {SteamId} {PlayerName} {IpAddress} {CountryCode} {PingMs}",
                    e.SteamId, e.PlayerName, e.IpAddress, e.CountryCode ?? "unknown", e.PingMs);
                break;

            case PlayerDisconnectEvent e:
                _logger.LogInformation(
                    "player.disconnect {SteamId} {PlayerName} {Team} {Reason} {SessionDurationSeconds}",
                    e.Player.SteamId, e.Player.PlayerName, e.Player.Team,
                    e.Reason, (int)e.SessionDuration.TotalSeconds);
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

            // ── Kills ─────────────────────────────────────────────────────────
            case KillEvent e:
                _logger.LogInformation(
                    "kill {AttackerSteamId} {AttackerName} {VictimSteamId} {VictimName} " +
                    "{Weapon} {IsHeadshot} {IsPenetration} {IsNoscope} {IsThroughSmoke} " +
                    "{IsAttackerBlind} {DistanceUnits} {IsTeamKill} {IsSuicide} {MapName} {RoundNumber}",
                    e.Attacker.SteamId, e.Attacker.PlayerName,
                    e.Victim.SteamId,   e.Victim.PlayerName,
                    e.Weapon, e.IsHeadshot, e.IsPenetration, e.IsNoscope,
                    e.IsThroughSmoke, e.IsAttackerBlind, e.DistanceUnits,
                    e.IsTeamKill, e.IsSuicide, e.MapName, e.RoundNumber);
                break;

            // ── Rounds / Match ────────────────────────────────────────────────
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

            // ── Bomb ──────────────────────────────────────────────────────────
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
                break;

            case BombExplodedEvent e:
                _logger.LogInformation(
                    "bomb.exploded {Site} {MapName} {RoundNumber}",
                    e.Site, e.MapName, e.RoundNumber);
                break;

            // ── Economy ───────────────────────────────────────────────────────
            case PlayerBuyEvent e:
                _logger.LogInformation(
                    "economy.buy {SteamId} {PlayerName} {Team} {Item} {Cost} {MoneyRemaining} {MapName} {RoundNumber}",
                    e.Player.SteamId, e.Player.PlayerName, e.Player.Team,
                    e.Item, e.Cost, e.MoneyRemaining, e.MapName, e.RoundNumber);
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

            // ── Chat ──────────────────────────────────────────────────────────
            case PlayerChatEvent e:
                _logger.LogInformation(
                    "chat.message {SteamId} {PlayerName} {Team} {Channel} {Message} {MapName} {RoundNumber}",
                    e.Player.SteamId, e.Player.PlayerName, e.Player.Team,
                    e.Channel, e.Message, e.MapName, e.RoundNumber);
                break;

            // ── Server ────────────────────────────────────────────────────────
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

    public void Dispose() => _loggerFactory.Dispose();
}
