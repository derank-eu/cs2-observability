using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Player;

public sealed record PlayerTeamChangeEvent(
    PlayerInfo Player,
    GameTeam FromTeam,
    GameTeam ToTeam,
    DateTimeOffset OccurredAt
) : IGameEvent;
