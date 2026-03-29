using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Player;

public sealed record PlayerConnectEvent(
    string SteamId,
    string PlayerName,
    string IpAddress,
    /// <summary>ISO 3166-1 alpha-2 country code resolved from IP, if available.</summary>
    string? CountryCode,
    /// <summary>ASN organisation name resolved from IP, if available.</summary>
    string? AsnOrganization,
    int PingMs,
    DateTimeOffset OccurredAt
) : IGameEvent;
