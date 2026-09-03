# Game Main-Thread Scheduler

`IDCMLGameThread` is the host-neutral DCML capability for safely marshalling
work onto the game/Unity main thread.

Capability:

```text
dcml.game.main-thread
```

## Why this exists

Unity and IL2CPP objects are not generally safe to inspect or manipulate from
arbitrary worker threads.

DCML therefore exposes an explicit scheduler before adding live hardware state
readers. Mods can check `IsMainThread`, queue fire-and-forget work with `Post`,
or await marshalled work with `InvokeAsync`.

```csharp
IDCMLGameThread? gameThread =
    context.Services.GetService(
        typeof(IDCMLGameThread))
    as IDCMLGameThread;

await gameThread!.InvokeAsync(
    () =>
    {
        // This callback executes on DCML's captured game thread.
    });
```

## Contract

```csharp
bool IsMainThread { get; }

void Post(Action action);

Task InvokeAsync(Action action);

Task<T> InvokeAsync<T>(Func<T> function);
```

The MelonLoader host captures the thread on which DCML initializes and drains
the shared queue from `MelonMod.OnUpdate`.

Posted callback failures are isolated so one mod cannot prevent later queued
work from executing. Awaited callbacks propagate their own exceptions through
the returned task.

The host currently drains at most 4,096 callbacks per update to prevent an
unbounded queue from monopolizing one frame.

## Loader compatibility

This is an optional capability. Mods are not required to use it merely to be
loadable by DCML. It is the recommended path for optional APIs that access
Unity/IL2CPP runtime state.

## Live proof

`DCML.TestModule` records:

```text
GameThreadProbeRuns
LastGameThreadInitializeWasMainThread
LastGameThreadBackgroundWasMainThread
LastGameThreadPostWasMainThread
LastGameThreadInvokeWasMainThread
LastGameThreadPostCount
LastGameThreadInvokeCount
LastGameThreadError
```

A healthy live run should show:

```text
LastGameThreadInitializeWasMainThread: True
LastGameThreadBackgroundWasMainThread: False
LastGameThreadPostWasMainThread: True
LastGameThreadInvokeWasMainThread: True
LastGameThreadPostCount: 1
LastGameThreadInvokeCount: 1
LastGameThreadError:
```

This proves that a worker-thread request was actually marshalled back onto the
captured game thread rather than merely executing inline.
