namespace MiniEventBus.Core;

public interface IEventBus
{
    void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
    bool Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
