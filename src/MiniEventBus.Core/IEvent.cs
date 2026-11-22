namespace MiniEventBus.Core;

public interface IEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredAt { get; }
}
