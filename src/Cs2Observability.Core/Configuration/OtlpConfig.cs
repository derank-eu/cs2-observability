using System.Text.Json.Serialization;

namespace Cs2Observability.Core.Configuration;

public sealed class OtlpConfig
{
    /// <summary>OTLP collector endpoint. Supports gRPC (http://host:4317) and HTTP (http://host:4318).</summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "http://otel-collector:4317";

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
