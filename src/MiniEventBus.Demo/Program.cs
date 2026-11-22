using MiniEventBus.Core;

var bus = new InMemoryEventBus();
bus.Subscribe(new EmailHandler());
bus.Subscribe(new StockHandler());
bus.Subscribe(new AuditHandler());

var order = new OrderPlaced(Guid.NewGuid(), DateTimeOffset.UtcNow, 150m);
Console.WriteLine($"Publishing order {order.Id}");
await bus.PublishAsync(order);

public sealed record OrderPlaced(Guid Id, DateTimeOffset OccurredAt, decimal Total) : IEvent;

public sealed class EmailHandler : IEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Email: confirmation for {@event.Id}");
        return Task.CompletedTask;
    }
}

public sealed class StockHandler : IEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Stock: processing order {@event.Id}");
        return Task.CompletedTask;
    }
}

public sealed class AuditHandler : IEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Audit: order {@event.Id}, total {@event.Total:C}");
        return Task.CompletedTask;
    }
}
