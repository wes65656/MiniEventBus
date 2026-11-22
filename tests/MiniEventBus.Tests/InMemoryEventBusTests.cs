using MiniEventBus.Core;

namespace MiniEventBus.Tests;

public sealed class InMemoryEventBusTests
{
    private sealed record TestEvent(Guid Id, DateTimeOffset OccurredAt) : IEvent;
    private sealed record OtherEvent(Guid Id, DateTimeOffset OccurredAt) : IEvent;

    private sealed class Handler<TEvent>(Func<TEvent, CancellationToken, Task> action)
        : IEventHandler<TEvent> where TEvent : IEvent
    {
        public Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
            => action(@event, cancellationToken);
    }

    private static TestEvent NewEvent() => new(Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public async Task Publish_delivers_to_all_subscribers_in_registration_order()
    {
        var bus = new InMemoryEventBus();
        var calls = new List<int>();
        bus.Subscribe(new Handler<TestEvent>((_, _) => { calls.Add(1); return Task.CompletedTask; }));
        bus.Subscribe(new Handler<TestEvent>((_, _) => { calls.Add(2); return Task.CompletedTask; }));

        await bus.PublishAsync(NewEvent());

        Assert.Equal(new[] { 1, 2 }, calls);
    }

    [Fact]
    public async Task Publish_ignores_handlers_for_other_event_types()
    {
        var bus = new InMemoryEventBus();
        var calls = 0;
        bus.Subscribe(new Handler<OtherEvent>((_, _) => { calls++; return Task.CompletedTask; }));

        await bus.PublishAsync(NewEvent());

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Unsubscribe_removes_handler()
    {
        var bus = new InMemoryEventBus();
        var calls = 0;
        var handler = new Handler<TestEvent>((_, _) => { calls++; return Task.CompletedTask; });
        bus.Subscribe(handler);

        Assert.True(bus.Unsubscribe(handler));
        Assert.False(bus.Unsubscribe(handler));
        await bus.PublishAsync(NewEvent());

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Handler_failure_stops_dispatch_and_propagates()
    {
        var bus = new InMemoryEventBus();
        var laterCalled = false;
        bus.Subscribe(new Handler<TestEvent>((_, _) => throw new InvalidOperationException("boom")));
        bus.Subscribe(new Handler<TestEvent>((_, _) => { laterCalled = true; return Task.CompletedTask; }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(NewEvent()));
        Assert.False(laterCalled);
    }

    [Fact]
    public async Task Cancellation_before_publish_prevents_dispatch()
    {
        var bus = new InMemoryEventBus();
        var called = false;
        bus.Subscribe(new Handler<TestEvent>((_, _) => { called = true; return Task.CompletedTask; }));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => bus.PublishAsync(NewEvent(), cts.Token));
        Assert.False(called);
    }

    [Fact]
    public async Task Handler_receives_cancellation_token()
    {
        var bus = new InMemoryEventBus();
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;
        bus.Subscribe(new Handler<TestEvent>((_, token) =>
        {
            received = token;
            return Task.CompletedTask;
        }));

        await bus.PublishAsync(NewEvent(), cts.Token);

        Assert.Equal(cts.Token, received);
    }

    [Fact]
    public async Task Subscribing_during_dispatch_applies_to_next_publication()
    {
        var bus = new InMemoryEventBus();
        var calls = 0;
        var second = new Handler<TestEvent>((_, _) => { calls++; return Task.CompletedTask; });
        bus.Subscribe(new Handler<TestEvent>((_, _) =>
        {
            bus.Subscribe(second);
            return Task.CompletedTask;
        }));

        await bus.PublishAsync(NewEvent());
        Assert.Equal(0, calls);
        await bus.PublishAsync(NewEvent());
        Assert.Equal(1, calls);
    }
}
