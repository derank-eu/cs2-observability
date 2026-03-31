using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Player;

public sealed record PlayerKickEvent(
    PlayerInfo Player,
    string Reason,
    /// <summary>SteamId of the admin who issued the kick. Null when kicked by the server.</summary>
    string? KickedBySteamId,
    DateTimeOffset OccurredAt
) : IGameEvent;
