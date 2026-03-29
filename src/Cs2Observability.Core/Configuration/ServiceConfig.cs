using System.Text.Json.Serialization;

namespace Cs2Observability.Core.Configuration;

public sealed class ServiceConfig
{
    /// <summary>Value of the OTel resource attribute service.name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "cs2-server";

    /// <summary>Arbitrary key/value pairs added as OTel resource attributes (e.g. server.region, server.id).</summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, string> Attributes { get; set; } = new();
}
