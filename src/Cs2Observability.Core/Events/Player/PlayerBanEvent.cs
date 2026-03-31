using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Player;

public sealed record PlayerBanEvent(
    string SteamId,
    string PlayerName,
    BanSource Source,
    string? Reason,
    /// <summary>Null indicates a permanent ban.</summary>
    TimeSpan? Duration,
    /// <summary>SteamId of the admin who issued the ban. Null for automated bans (VAC, server).</summary>
    string? BannedBySteamId,
    DateTimeOffset OccurredAt
) : IGameEvent;
