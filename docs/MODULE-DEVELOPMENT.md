# DCML Module Development

## Core contract

A DCML module implements `IDCMLModule`:

```csharp
public interface IDCMLModule
{
    string Id { get; }
    string Name { get; }
    string Version { get; }

    void Initialize(IDCMLModuleContext context);
    void Start();
    void Stop();
}
```

The runtime verifies that the module instance ID matches its manifest ID.

## Context

`Initialize` receives the module context:

```csharp
public interface IDCMLModuleContext
{
    string ModuleDirectory { get; }
    string DataDirectory { get; }
    IServiceProvider Services { get; }
}
```

`ModuleDirectory` is the installed package directory.

`DataDirectory` is persistent module-owned storage.

## Logging

```csharp
var logger =
    context.Services.GetService(typeof(IDCMLLogger))
        as IDCMLLogger;

logger?.Info("Module initialized.");
logger?.Warning("Example warning.");
logger?.Error("Example error.");
```

## Runtime information

```csharp
var runtime =
    context.Services.GetService(typeof(IDCMLRuntimeInfo))
        as IDCMLRuntimeInfo;

if (runtime?.HasCapability(
        DCMLRuntimeCapabilities.Events) == true)
{
    // Event bus is available.
}
```

Current runtime-information fields include:

- `ModuleId`
- `DCMLVersion`
- `HostName`
- `HostVersion`
- `GameName`
- `GameRoot`
- `Capabilities`

The original string capability API remains supported.

### Versioned capability checks

Newer modules that require a particular API revision can request
`IDCMLCapabilityCatalog`:

```csharp
var capabilities =
    context.Services.GetService(typeof(IDCMLCapabilityCatalog))
        as IDCMLCapabilityCatalog;

if (capabilities?.SupportsCapability(
        DCMLRuntimeCapabilities.Events,
        "1.0.0") == true)
{
    // Event API 1.0.0 or newer is available.
}
```

Capability versions are independent from the overall DCML release version.
All capability contracts that existed before the versioned catalog begin at
capability API version `1.0.0`.

If a module only needs the original unversioned behavior, it may fall back to
`IDCMLRuntimeInfo.HasCapability(...)` on older hosts. If it requires behavior
from a newer capability API revision, absence of `IDCMLCapabilityCatalog`
should be treated as unsupported.

See
[Versioned Runtime Capabilities](VERSIONED-RUNTIME-CAPABILITIES.md)
for the compatibility and fallback rules.

## Configuration

```csharp
var configuration =
    context.Services.GetService(typeof(IDCMLConfiguration))
        as IDCMLConfiguration;

var settings =
    configuration!.Load(
        new Settings());

settings.Enabled = true;

configuration.Save(
    settings);
```

The current host stores configuration at:

```text
UserData\DCML\Data\<module-id>\config.json
```

## Events

```csharp
var eventBus =
    context.Services.GetService(typeof(IDCMLEventBus))
        as IDCMLEventBus;

IDisposable subscription =
    eventBus!.Subscribe<MyEvent>(
        value =>
        {
            // Handle value.
        });

eventBus.Publish(
    new MyEvent());

subscription.Dispose();
```

One event bus is shared across the runtime, allowing modules to communicate without direct assembly references when they share an event contract type.

## Manifest

Example:

```json
{
  "schemaVersion": 1,
  "id": "example.module",
  "name": "Example Module",
  "version": "0.1.0",
  "entryAssembly": "ExampleModule.dll",
  "entryType": "Example.Module",
  "minimumDCMLVersion": "0.0.1",
  "requiresRestart": false,
  "dependencies": []
}
```

Dependencies may be required or optional and may declare minimum versions.

## Compatibility

Modules should prefer DCML capabilities over direct host dependencies when
the DCML abstraction fits their needs.

For example, `IDCMLLogger` can be used instead of calling `MelonLogger`
directly. That allows the same module code to work through another host
adapter in the future.

This is a recommendation, not a loader requirement. A compatible mod may use
lower-level host, Unity, IL2CPP, or game APIs directly.
