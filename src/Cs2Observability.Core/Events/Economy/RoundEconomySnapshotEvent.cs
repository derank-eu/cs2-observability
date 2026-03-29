using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Economy;

/// <summary>
/// Snapshot of both teams' economy at the start of the buy phase.
/// Emitted once per round, after freeze time begins.
/// </summary>
public sealed record RoundEconomySnapshotEvent(
    string MapName,
    int RoundNumber,
    TeamEconomyInfo Terrorists,
    TeamEconomyInfo CounterTerrorists,
    DateTimeOffset OccurredAt
) : IGameEvent;
