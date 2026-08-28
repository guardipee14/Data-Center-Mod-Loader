# Data Center Mod Loader (DCML)

DCML is an experimental, host-neutral module runtime for **Data Center** by Waseku.

The current prototype runs through a MelonLoader host adapter, but DCML modules target the DCML Core API instead of directly inheriting from MelonLoader. The goal is to keep module code portable across future host implementations.

> **Project status:** early development / v0.0.1 runtime foundation.

## What works today

The current runtime has been tested inside the live Data Center game process and can:

- discover module packages from `UserData\DCML\Modules`;
- parse and validate `manifest.json`;
- validate semantic versions;
- resolve required and optional dependencies;
- enforce minimum dependency versions;
- detect dependency cycles;
- produce deterministic dependency-safe load order;
- dynamically load module assemblies;
- activate `IDCMLModule` implementations;
- run `Initialize`, `Start`, and `Stop`;
- isolate module failures;
- stop modules in reverse startup order;
- persist per-module data;
- expose host-neutral services through `IDCMLModuleContext.Services`.

The current automated test baseline is **80 passing tests**.

## Live-proven services

DCML currently exposes four host-neutral services:

| Capability | Service | Purpose |
| --- | --- | --- |
| `dcml.logging` | `IDCMLLogger` | Module-scoped logging without a MelonLoader dependency |
| `dcml.runtime-information` | `IDCMLRuntimeInfo` | DCML, host, game, module, and capability information |
| `dcml.configuration` | `IDCMLConfiguration` | Typed persistent JSON configuration |
| `dcml.events` | `IDCMLEventBus` | Shared typed publish/subscribe event bus |

The end-to-end probe has verified logging, configuration persistence across separate game launches, typed event delivery, and clean shutdown inside Data Center.

## Architecture

```text
Data Center
    |
MelonLoader
    |
DCML.Loader.MelonLoader
    |
DCML.Core
    |
Package discovery
    |
Manifest validation
    |
Dependency resolution
    |
Module activation
    |
IDCMLModule
    |
Initialize -> Start -> Stop
```

Modules only depend on DCML Core:

```text
DCML module
    |
IDCMLModuleContext.Services
    |
+-- IDCMLLogger
+-- IDCMLRuntimeInfo
+-- IDCMLConfiguration
+-- IDCMLEventBus
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for more detail.

## Module package format

A module package is a directory containing a manifest and entry assembly:

```text
UserData/
  DCML/
    Modules/
      ExampleModule/
        manifest.json
        ExampleModule.dll
```

Example manifest:

```json
{
  "schemaVersion": 1,
  "id": "example.module",
  "name": "Example Module",
  "version": "0.1.0",
  "description": "Example DCML module.",
  "author": "Example",
  "entryAssembly": "ExampleModule.dll",
  "entryType": "Example.Module",
  "minimumDCMLVersion": "0.0.1",
  "requiresRestart": false,
  "dependencies": []
}
```

## Minimal module

```csharp
using DCML.Core.Abstractions;

public sealed class Module : IDCMLModule
{
    private IDCMLLogger? _logger;

    public string Id => "example.module";
    public string Name => "Example Module";
    public string Version => "0.1.0";

    public void Initialize(IDCMLModuleContext context)
    {
        _logger =
            context.Services.GetService(typeof(IDCMLLogger))
                as IDCMLLogger;

        _logger?.Info("Initialized.");
    }

    public void Start()
    {
        _logger?.Info("Started.");
    }

    public void Stop()
    {
        _logger?.Info("Stopped.");
    }
}
```

See [docs/MODULE-DEVELOPMENT.md](docs/MODULE-DEVELOPMENT.md) for the current module API.

## Projects

```text
src/
  DCML.Core/
  DCML.Loader.MelonLoader/
  DCML.TestModule/

tests/
  DCML.Core.Tests/
```

### DCML.Core

Host-neutral contracts, package/runtime models, discovery, validation, dependency resolution, lifecycle coordination, services, and shared utilities.

Targets:

- `netstandard2.1`
- `net6.0`

### DCML.Loader.MelonLoader

Current Data Center host adapter.

Targets:

- `net6.0`

It is responsible for connecting MelonLoader to DCML Core and supplying host-backed services.

### DCML.TestModule

End-to-end probe module used to prove the runtime in the actual game process.

## Building

The current development environment uses the .NET SDK and a local Data Center installation containing MelonLoader.

Example:

```powershell
dotnet build .\DCML.sln `
    -c Release `
    "-p:DataCenterRoot=C:\Program Files (x86)\Steam\steamapps\common\Data Center"
```

Run tests:

```powershell
dotnet test .\DCML.sln `
    -c Release `
    "-p:DataCenterRoot=C:\Program Files (x86)\Steam\steamapps\common\Data Center"
```

## Current limitations

- The working host adapter currently requires MelonLoader to already be present.
- A Boosteroid/cloud-gaming first-stage bootstrap is **not solved** by this repository.
- Data Center-specific gameplay APIs have not yet been exposed through host-neutral DCML abstractions.
- The API is still early and may change before a stable v0.1 release.

## Next direction

The next major milestone is the first **Data Center-facing API abstraction**, built above the host-neutral runtime and services.

Planned areas include:

- game lifecycle/events;
- object and entity discovery;
- networking/infrastructure information;
- safe interaction abstractions;
- capability-based APIs so modules can adapt to available hosts.

## Disclaimer

DCML is an independent community project and is not affiliated with Waseku, Data Center, MelonLoader, Steam, or Valve.
