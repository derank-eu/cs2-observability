using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Player;

public sealed record PlayerMuteEvent(
    PlayerInfo Player,
    bool IsMuted,
    string? MutedBySteamId,
    DateTimeOffset OccurredAt
) : IGameEvent;
