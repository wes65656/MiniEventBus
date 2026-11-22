# MiniEventBus

A small educational **in-memory event bus** built with **C# / .NET 10**, without third-party messaging frameworks.

## Features

- Typed event contracts and handlers
- Multiple subscribers per event type
- Subscribe / unsubscribe
- Asynchronous, **sequential** dispatch in registration order
- CancellationToken propagation
- Snapshot-based dispatch (changes to subscriptions affect the next publication)
- Thread-safe subscription registry
- Fail-fast error handling: a handler exception propagates and later handlers are skipped

## Run

Install the .NET 10 SDK, then:

```bash
dotnet run --project src/MiniEventBus.Demo
dotnet test tests/MiniEventBus.Tests
```

## Architecture

`MiniEventBus.Core` contains the contracts and in-process implementation.
`MiniEventBus.Demo` publishes an order event to email, stock and audit handlers.
`MiniEventBus.Tests` contains xUnit tests.

## Limitations

This is **not** a distributed message broker. It does not persist events, retry failed handlers, provide exactly-once delivery, or coordinate transactions. Subscriptions and undelivered events are lost when the process exits. Handlers are called sequentially and a failure stops subsequent handlers. Registering the same handler twice invokes it twice.

## Status

Source and tests were authored through GitHub. Automated tests still need to be executed in an environment with the .NET 10 SDK.
