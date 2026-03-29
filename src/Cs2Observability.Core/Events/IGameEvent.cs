namespace Cs2Observability.Core.Events;

/// <summary>
/// Marker interface for all CS2 game events produced by the observability plugin.
/// Every event carries the UTC timestamp of when it occurred.
/// </summary>
public interface IGameEvent
{
    DateTimeOffset OccurredAt { get; }
}
