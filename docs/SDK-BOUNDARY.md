# Optional SDK Boundary

DCML separates the minimal loader/runtime contract from optional developer
conveniences.

The goal is to keep module acceptance small and host-independent while giving
module authors an ergonomic API when they choose to use one.

## Minimal loader/runtime contract

A DCML module does not need DCML.SDK, DCML.DataCenter, MelonLoader helper
types, or any convenience service merely to be discovered and activated.

The minimal module-facing contract is:

```text
IDCMLModule
    |
    +-- Initialize(IDCMLModuleContext)
    +-- Start()
    +-- Stop()

IDCMLModuleContext
    |
    +-- ModuleDirectory
    +-- DataDirectory
    +-- IServiceProvider Services
```

`IDCMLModule` and `IDCMLModuleContext` remain in `DCML.Core`.

Host/runtime contracts such as `IDCMLModuleActivator` and
`IDCMLModuleContextFactory` also remain in Core because they define the
loader/runtime boundary rather than developer ergonomics.

## Optional runtime service contracts

The following contracts remain in `DCML.Core.Abstractions` so existing modules
keep their current type identity and source/binary compatibility:

```text
IDCMLLogger
IDCMLConfiguration
IDCMLEventBus
IDCMLRuntimeInfo
IDCMLCapabilityCatalog
IDCMLGameLifecycle
IDCMLGameObjectDiscovery
IDCMLGameResourceDiscovery
IDCMLGameTypeCatalog
IDCMLGameTypeInspector
IDCMLGameThread
IDCMLGameComponentStateReader
```

These are optional services exposed through `IDCMLModuleContext.Services`.
Their presence is host/capability dependent.

Moving these interfaces into another assembly would change their .NET type
identity and would unnecessarily break existing modules. DCML therefore keeps
the contracts in Core.

## What belongs in DCML.SDK

`DCML.SDK` is an optional convenience layer that references `DCML.Core`.

It may provide:

- typed service lookup helpers;
- concise patterns for optional and required services;
- developer-oriented wrappers and extension methods;
- examples and guidance that reduce repetitive `IServiceProvider` code.

It must not:

- become a loader acceptance requirement;
- be referenced by `DCML.Core`;
- be required by a module that only implements the minimal loader contract;
- move existing Core service interfaces and break compatibility;
- imply a capability requirement that a package did not explicitly declare.

The first SDK surface provides:

```text
TryGetService<TService>()
GetOptionalService<TService>()
GetRequiredService<TService>()
```

All three operate on `IDCMLModuleContext` and resolve the same service objects
that are available through `context.Services`.

## Backwards-compatible access

Existing code remains valid:

```csharp
IDCMLLogger? logger =
    context.Services.GetService(
        typeof(IDCMLLogger))
    as IDCMLLogger;
```

A module that references `DCML.SDK` may instead write:

```csharp
IDCMLLogger? logger =
    context.GetOptionalService<IDCMLLogger>();
```

Both forms use the same Core contract and the same host service provider.

## Required versus optional services

SDK helpers do not decide whether a service is a package requirement.

For optional behavior:

```csharp
IDCMLLogger? logger =
    context.GetOptionalService<IDCMLLogger>();

logger?.Info(
    "Optional logging is available.");
```

For a service the module has explicitly chosen to require:

```csharp
IDCMLLogger logger =
    context.GetRequiredService<IDCMLLogger>();
```

Package compatibility requirements still belong in the package manifest
through `requiredCapabilities`.

## Examples

### Logging

```csharp
IDCMLLogger? logger =
    context.GetOptionalService<IDCMLLogger>();

logger?.Info(
    "Module initialized.");
```

### Configuration

```csharp
IDCMLConfiguration? configuration =
    context.GetOptionalService<IDCMLConfiguration>();

if (configuration is not null)
{
    MySettings settings =
        configuration.Load(
            new MySettings());
}
```

### Events

```csharp
IDCMLEventBus? events =
    context.GetOptionalService<IDCMLEventBus>();

events?.Publish(
    new MyModuleReadyEvent());
```

### Scene lifecycle

```csharp
IDCMLGameLifecycle? lifecycle =
    context.GetOptionalService<IDCMLGameLifecycle>();

if (
    lifecycle is not null &&
    lifecycle.HasCurrentScene)
{
    string scene =
        lifecycle.CurrentSceneName;
}
```

### Main-thread work

```csharp
IDCMLGameThread? gameThread =
    context.GetOptionalService<IDCMLGameThread>();

if (gameThread is not null)
{
    await gameThread.InvokeAsync(
        () =>
        {
            // Unity/game-facing work.
        });
}
```

### Object discovery

```csharp
IDCMLGameObjectDiscovery? discovery =
    context.GetOptionalService<IDCMLGameObjectDiscovery>();

if (discovery is not null)
{
    IReadOnlyList<DCMLGameObjectInfo> objects =
        discovery.Find(
            new DCMLGameObjectQuery(
                nameContains:
                    "Server"));
}
```

### Resource discovery

```csharp
IDCMLGameResourceDiscovery? resources =
    context.GetOptionalService<IDCMLGameResourceDiscovery>();

if (resources is not null)
{
    IReadOnlyList<DCMLGameResourceInfo> matches =
        resources.Find(
            new DCMLGameResourceQuery());
}
```

### Type discovery and inspection

```csharp
IDCMLGameTypeCatalog? types =
    context.GetOptionalService<IDCMLGameTypeCatalog>();

IDCMLGameTypeInspector? inspector =
    context.GetOptionalService<IDCMLGameTypeInspector>();
```

### Runtime information and capability catalog

```csharp
IDCMLRuntimeInfo? runtime =
    context.GetOptionalService<IDCMLRuntimeInfo>();

IDCMLCapabilityCatalog? capabilities =
    context.GetOptionalService<IDCMLCapabilityCatalog>();
```

## DCML.DataCenter remains a separate optional domain API

`DCML.DataCenter` is not part of the minimal loader contract and is not folded
into `DCML.SDK`.

It is a Data Center-specific semantic layer built on optional Core game
services such as object discovery, type discovery, and component-state
reading.

This preserves a clean dependency direction:

```text
DCML.Core
    ^
    |
DCML.SDK

DCML.Core
    ^
    |
DCML.DataCenter
```

Neither optional layer is required for loader acceptance.

## Stability policy

For v0.0.4:

- the minimal loader/runtime contracts remain in Core;
- existing optional service interfaces remain in Core;
- DCML.SDK is additive and optional;
- DCML.DataCenter remains additive and optional;
- direct `IServiceProvider` access remains supported;
- SDK helper APIs are provisional until the public capability/API reference is
  completed.

The separate API-reference roadmap item remains open and will document the
stable versus provisional capability surface in detail.
