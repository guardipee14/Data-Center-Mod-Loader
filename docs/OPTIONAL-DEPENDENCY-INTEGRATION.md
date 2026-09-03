# Optional Dependency Package Integration

DCML v0.0.4 validates optional dependency behavior with real module packages in
both the **provider absent** and **provider present** layouts.

## Packages

```text
dcml.probe.optional-consumer
dcml.probe.optional-provider
```

The consumer manifest declares:

```json
{
  "id": "dcml.probe.optional-provider",
  "minimumVersion": "1.0.0",
  "optional": true
}
```

## Current resolver semantics

Optional dependencies are not required-dependency edges.

That means:

- a missing optional provider does not block the consumer;
- an optional provider does not impose dependency-safe startup ordering;
- the consumer must tolerate either startup order if it chooses to integrate
  with the provider at runtime.

This is intentionally different from a required dependency.

## Provider absent proof

The integration test stages only the consumer package.

It then uses the normal:

```text
DCMLPackageDiscovery
DCMLDependencyResolver
DCMLReflectionModuleActivator
DCMLModuleRuntime
```

path.

Expected result:

- one valid package;
- zero discovery failures;
- zero dependency-resolution issues;
- consumer state `Running`;
- consumer trace records `ConsumerRunning`;
- no `OptionalProviderObserved` event.

## Provider present proof

The integration test stages both real packages.

Because the optional dependency does not create a load-order edge, deterministic
ID ordering currently starts:

```text
dcml.probe.optional-consumer
dcml.probe.optional-provider
```

The consumer therefore starts before the provider in this probe.

The consumer publishes a query when it starts. If the provider is already
running, it responds. If the provider has not started yet, the query is simply
ignored.

When the provider starts, it publishes a presence announcement. The already
running consumer receives that announcement and records:

```text
OptionalProviderObserved
```

This makes the runtime integration tolerant of either optional-package startup
order without changing optional dependencies into required dependencies.

## Scope

These probes use only DCML Core contracts. They do not require
`DCML.DataCenter`, MelonLoader APIs, scene discovery, save access, hardware
inspection, or game-state mutation.

Optional dependencies remain a package-declaration feature. They do not make
use of optional DCML SDK/Data Center APIs mandatory for loader acceptance.
