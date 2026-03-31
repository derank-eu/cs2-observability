using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Player;

public sealed record PlayerDisconnectEvent(
    PlayerInfo Player,
    /// <summary>Raw disconnect reason string from the engine (e.g. "Disconnect", "TimedOut", "STEAM_VALIDATION_REJECTED").</summary>
    string Reason,
    TimeSpan SessionDuration,
    DateTimeOffset OccurredAt
) : IGameEvent;
