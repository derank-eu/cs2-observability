using Cs2Observability.Core.Enums;
using Cs2Observability.Core.Events;
using Cs2Observability.Core.Shared;

namespace Cs2Observability.Core.Events.Chat;

public sealed record PlayerChatEvent(
    PlayerInfo Player,
    string Message,
    ChatChannel Channel,
    string MapName,
    int RoundNumber,
    DateTimeOffset OccurredAt
) : IGameEvent;
