using System.Collections.Concurrent;

namespace MiniEventBus.Core;

/// <summary>
/// An in-process, sequential, fail-fast event dispatcher.
/// Subscriptions are retained in memory and do not survive process restarts.
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, object> _subscriptions = new();

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        var handlers = (HandlerRegistry<TEvent>)_subscriptions.GetOrAdd(
            typeof(TEvent), static _ => new HandlerRegistry<TEvent>());
        handlers.Add(handler);
    }

    public bool Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        return _subscriptions.TryGetValue(typeof(TEvent), out var registry)
            && ((HandlerRegistry<TEvent>)registry).Remove(handler);
    }

    public async Task PublishAsync<TEvent>(
        TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_subscriptions.TryGetValue(typeof(TEvent), out var registry))
            return;

        foreach (var handler in ((HandlerRegistry<TEvent>)registry).Snapshot())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class HandlerRegistry<TEvent> where TEvent : IEvent
    {
        private readonly object _gate = new();
        private readonly List<IEventHandler<TEvent>> _handlers = new();

        public void Add(IEventHandler<TEvent> handler)
        {
            lock (_gate) _handlers.Add(handler);
        }

        public bool Remove(IEventHandler<TEvent> handler)
        {
            lock (_gate) return _handlers.Remove(handler);
        }

        public IEventHandler<TEvent>[] Snapshot()
        {
            lock (_gate) return _handlers.ToArray();
        }
    }
}
