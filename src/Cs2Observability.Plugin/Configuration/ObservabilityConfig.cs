using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Cs2Observability.Plugin.Configuration;

public sealed class ObservabilityConfig : BasePluginConfig
{
    [JsonPropertyName("otlp")]
    public OtlpConfig Otlp { get; set; } = new();

    [JsonPropertyName("service")]
    public ServiceConfig Service { get; set; } = new();

    [JsonPropertyName("events")]
    public EventsConfig Events { get; set; } = new();
}

public sealed class OtlpConfig
{
    /// <summary>OTLP collector endpoint. Supports gRPC (http://host:4317) and HTTP (http://host:4318).</summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>"grpc" or "http/protobuf"</summary>
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "grpc";

    /// <summary>Optional static headers to attach to every OTLP request (e.g. auth tokens).</summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>How long to wait for the collector before timing out, in milliseconds.</summary>
    [JsonPropertyName("timeout_ms")]
    public int TimeoutMs { get; set; } = 5000;
}

public sealed class ServiceConfig
{
    /// <summary>Value of the OTel resource attribute service.name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "cs2-server";

    /// <summary>Arbitrary key/value pairs added as OTel resource attributes (e.g. server.region, server.id).</summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = new();
}

/// <summary>
/// Fine-grained toggles for each event category.
/// Disabling a category prevents both the CSS hook registration and any export work.
/// </summary>
public sealed class EventsConfig
{
    [JsonPropertyName("player")]
    public bool Player { get; set; } = true;

    [JsonPropertyName("kills")]
    public bool Kills { get; set; } = true;

    [JsonPropertyName("rounds")]
    public bool Rounds { get; set; } = true;

    [JsonPropertyName("bomb")]
    public bool Bomb { get; set; } = true;

    [JsonPropertyName("economy")]
    public bool Economy { get; set; } = true;

    [JsonPropertyName("chat")]
    public bool Chat { get; set; } = true;

    [JsonPropertyName("server")]
    public bool Server { get; set; } = true;
}
