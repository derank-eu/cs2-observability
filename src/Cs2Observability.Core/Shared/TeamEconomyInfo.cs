using Cs2Observability.Core.Enums;

namespace Cs2Observability.Core.Shared;

public sealed record TeamEconomyInfo(
    GameTeam Team,
    int AverageMoneyAmount,
    int TotalEquipmentValue,
    int PlayerCount
);
