using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Game;

public sealed record KillEvent(
    PlayerInfo Attacker,
    PlayerInfo Victim,
    string Weapon,
    bool IsHeadshot,
    bool IsPenetration,
    bool IsNoscope,
    bool IsThroughSmoke,
    bool IsAttackerBlind,
    float DistanceUnits,
    bool IsTeamKill,
    bool IsSuicide,
    string MapName,
    int RoundNumber,
    DateTimeOffset OccurredAt
) : IGameEvent;
