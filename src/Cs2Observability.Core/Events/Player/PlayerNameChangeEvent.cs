using Cs2Observability.Core.Events;

namespace Cs2Observability.Core.Events.Player;

public sealed record PlayerNameChangeEvent(
    string SteamId,
    string OldName,
    string NewName,
    DateTimeOffset OccurredAt
) : IGameEvent;
