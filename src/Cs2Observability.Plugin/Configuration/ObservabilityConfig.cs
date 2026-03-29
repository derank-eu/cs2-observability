using System.Text.Json.Serialization;
using Cs2Observability.Core.Configuration;
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
