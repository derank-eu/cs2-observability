using Cs2Observability.Core.Enums;

namespace Cs2Observability.Core.Shared;

/// <summary>Snapshot of a player's identity and team at the time of an event.</summary>
public sealed record PlayerInfo(
    string SteamId,
    string PlayerName,
    GameTeam Team
);
