# Data Center Mod Loader (DCML)

DCML is an experimental, host-neutral mod runtime for **Data Center** by Waseku.

The current implementation runs through a MelonLoader host adapter, while DCML's runtime contracts remain separate from MelonLoader. The goal is to let the loader handle discovery, validation, dependency ordering, activation, lifecycle, and host bridging without forcing every mod author into a special high-level development framework.

> **Project status:** early development / **v0.0.4 development — Data Center integration complete**.

[Latest prerelease: DCML v0.0.3](https://github.com/guardipee14/Data-Center-Mod-Loader/releases/tag/v0.0.3)

## Modding model

DCML's primary job is to **load compatible mods and bridge them into Data Center in a form the current game/runtime host can understand**.

A mod does **not** have to use the optional Data Center helper APIs just to be loadable by DCML.

Developers may choose to:

- use the minimal DCML runtime contracts;
- use the optional `DCML.DataCenter` convenience API;
- use lower-level compatible Unity, IL2CPP, game, or host APIs directly;
- mix those approaches when appropriate.

The recommended helpers exist to reduce boilerplate and isolate mods from unstable game internals, not to become a loader requirement.

See [docs/MODDING-MODEL.md](docs/MODDING-MODEL.md) for the current compatibility model.

## What works today

The current runtime has been tested inside the live Data Center game process and can:

- discover module packages from `UserData\DCML\Modules`;
- parse and validate `manifest.json`;
- validate semantic versions;
- resolve required and optional dependencies;
- enforce minimum dependency versions;
- detect duplicate IDs and dependency cycles;
- produce deterministic dependency-safe load order;
- dynamically load module assemblies;
- activate `IDCMLModule` implementations;
- run `Initialize`, `Start`, and `Stop`;
- isolate module failures;
- stop modules in reverse startup order;
- persist per-module data;
- expose host-neutral services through `IDCMLModuleContext.Services`;
- receive Data Center scene lifecycle events;
- discover live Unity GameObjects;
- preserve native IL2CPP component identities;
- filter object discovery by exact component type or component prefix;
- page deterministically across large scenes;
- catalog loaded IL2CPP wrapper types and their inheritance information;
- provide optional Data Center-specific semantic discovery helpers;
- capture evidence-backed hardware snapshots and topology;
- preserve persistent physical-cable identities from explicit read-only save data;
- merge persisted cable segments into bidirectional physical `NetworkConnection` edges;
- keep save decoding outside the MelonLoader .NET 6 process through the optional .NET 8 persistence helper.

The current development baseline is **400 passing tests**.

## Runtime capabilities

The MelonLoader host currently advertises:

| Capability | Service | Purpose |
| --- | --- | --- |
| `dcml.logging` | `IDCMLLogger` | Module-scoped logging without requiring a MelonLoader dependency in module code |
| `dcml.runtime-information` | `IDCMLRuntimeInfo` | DCML, host, game, module, and capability information |
| `dcml.configuration` | `IDCMLConfiguration` | Typed persistent JSON configuration |
| `dcml.events` | `IDCMLEventBus` | Shared typed publish/subscribe event bus |
| `dcml.game.scene-lifecycle` | `IDCMLGameLifecycle` | Read-only scene loaded / initialized / unloaded lifecycle state and events |
| `dcml.game.object-discovery` | `IDCMLGameObjectDiscovery` | Read-only live GameObject and component discovery |
| `dcml.game.type-catalog` | `IDCMLGameTypeCatalog` | Read-only catalog of loaded runtime/IL2CPP wrapper types and inheritance metadata |

The end-to-end probe has verified runtime initialization, configuration persistence across game launches, event delivery, scene lifecycle delivery, object discovery, complete paged IL2CPP component inventory, game type catalog access, and clean shutdown inside Data Center.

## Live validation

The v0.0.3 physical-topology milestone was validated with the known healthy recovered save and the live Data Center runtime:

- **686** persisted physical cables;
- **1,372 / 1,372** cable endpoints resolved;
- **686** physical `NetworkConnection` edges;
- all persisted physical edges marked bidirectional;
- explicit save selection and SHA-256-gated validation;
- read-only save access;
- NRBF decoding isolated in an out-of-process .NET 8 helper;
- no `System.Formats.Nrbf` or `System.Reflection.Metadata 9` dependency loaded into the MelonLoader .NET 6 context;
- one-shot runtime proof completed and disabled itself afterward;
- no DCML runtime initialization errors in the verified launch.

The earlier v0.0.2 discovery milestone was validated inside Data Center with:

- **22,860** relevant `BaseScene` objects scanned across **2 pages**;
- **94** unique focused IL2CPP component types;
- a complete focused component inventory;
- **1,036** loaded `Il2Cpp.*` runtime types;
- no type-catalog result-limit hit at the 16,384-result bound;
- live semantic classification of `Il2Cpp.CableLink` as `cable`;
- no DCML runtime error in the verified run.

The loaded type catalog also confirmed gameplay types including:

- `Il2Cpp.Server`;
- `Il2Cpp.Rack`;
- `Il2Cpp.NetworkSwitch`;
- `Il2Cpp.Router : Il2Cpp.NetworkSwitch`;
- `Il2Cpp.Firewall : Il2Cpp.NetworkSwitch`;
- `Il2Cpp.CableLink`.

The tested BaseScene state did not contain matching instantiated `Server`, `Rack`, or network-device components, so DCML does not fabricate live entities for them.

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
+-- Package discovery
+-- Manifest validation
+-- Dependency resolution
+-- Deterministic load order
+-- Module activation
+-- Initialize -> Start -> Stop
+-- Host-neutral service contracts

Optional development layer
    |
DCML.DataCenter
    |
+-- Semantic entity discovery
+-- Component inventory
+-- Evidence-backed Data Center helpers
```

The loader/runtime remains separate from the optional Data Center-specific helper library.

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

## Optional Data Center helper API

`DCML.DataCenter` is an optional/recommended development layer above the low-level runtime services.

Current semantic entity kinds include:

- `user-interface`;
- `rack`;
- `server`;
- `network-device`;
- `cable`;
- `machine`;
- `unknown`.

Evidence-backed default mappings currently include:

| Runtime component | Semantic kind |
| --- | --- |
| `Il2Cpp.Server` | `server` |
| `Il2Cpp.Rack` | `rack` |
| `Il2Cpp.NetworkSwitch` | `network-device` |
| `Il2Cpp.Router` | `network-device` |
| `Il2Cpp.Firewall` | `network-device` |
| `Il2Cpp.CableLink` | `cable` |

Rules can also use inheritance-aware matching when `IDCMLGameTypeCatalog` is available.

DCML intentionally does **not** classify `RackMount` as a rack or `SFPModule` as an entire network device. No default machine/factory/hacking/coding classifications are added without supporting runtime evidence.

Relevant documentation:

- [Game Type Catalog](docs/GAME-TYPE-CATALOG.md)
- [Paged IL2CPP Inventory](docs/PAGED-IL2CPP-INVENTORY.md)
- [Evidence-Backed Hardware Classification](docs/EVIDENCE-BACKED-HARDWARE-CLASSIFICATION.md)
- [Inheritance-Aware Semantic Discovery](docs/INHERITANCE-AWARE-SEMANTIC-DISCOVERY.md)
- [Targeted Semantic Live Proof](docs/TARGETED-SEMANTIC-LIVE-PROOF.md)

## Projects

```text
src/
  DCML.Core/
  DCML.DataCenter/
  DCML.Loader.MelonLoader/
  DCML.Persistence.Helper/
  DCML.TestModule/

tests/
  DCML.Core.Tests/
```

### DCML.Core

Host-neutral contracts, package/runtime models, discovery, validation, dependency resolution, lifecycle coordination, shared services, scene/object discovery contracts, and runtime type-catalog contracts.

Targets:

- `netstandard2.1`
- `net6.0`

### DCML.DataCenter

Optional/recommended Data Center-specific helper library built on the low-level DCML Core discovery services.

Targets:

- `netstandard2.1`
- `net6.0`

### DCML.Loader.MelonLoader

Current Data Center host adapter.

Target:

- `net6.0`

It connects MelonLoader and IL2CPP runtime behavior to DCML Core without making MelonLoader part of the host-neutral module contract.

### DCML.TestModule

End-to-end probe module used to prove the runtime and read-only game-facing APIs inside the actual game process.

## Installing the current prerelease

Download `DCML-v0.0.3.zip` from the [v0.0.3 release](https://github.com/guardipee14/Data-Center-Mod-Loader/releases/tag/v0.0.3).

The archive contains the current MelonLoader host, shared DCML libraries, validation module, and optional persistence helper used by the v0.0.3 physical-topology release.

```text
Mods/
  DCML.Loader.MelonLoader.dll

UserLibs/
  DCML.Core.dll
  DCML.DataCenter.dll

UserData/
  DCML/
    Modules/
      DCML.TestModule/
        DCML.DataCenter.dll
        DCML.TestModule.dll
        manifest.json
        PersistenceHelper/
          DCML.Persistence.Helper.dll
          DCML.Persistence.Helper.deps.json
          DCML.Persistence.Helper.runtimeconfig.json
          System.Formats.Nrbf.dll
          System.Reflection.Metadata.dll
          System.Collections.Immutable.dll
          ...
```

Extract the archive into the Data Center game directory with MelonLoader already installed.

`DCML.TestModule` is a validation module and can be removed after verifying the runtime.

Published v0.0.3 ZIP SHA-256:

```text
1a5e8353c0ddefe99c0126741f77df2347a4847bf346b6e84181948ba7d15e2f
```

A matching `DCML-v0.0.3.sha256` file is attached to the GitHub release.

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

Current baseline:

```text
400 passed
0 failed
0 skipped
```

## Current limitations

- The working host adapter requires MelonLoader to already be present.
- A Boosteroid/cloud-gaming first-stage bootstrap is **not solved** by this repository.
- Current game-facing APIs are primarily read-only discovery/introspection APIs; a general safe game-control/write API has not been exposed yet.
- Semantic classifications are intentionally conservative and evidence-backed rather than guessed from object names.
- Some gameplay concepts may exist as types, prefabs, resources, ECS/DOTS entities, data/config models, or other structures rather than instantiated GameObject components.
- The API is still early and may change before a stable v0.1.0 release.

## Next direction

The next discovery work is expected to look beyond instantiated scene GameObjects and inspect read-only loaded resources/prefabs so DCML can understand hardware definitions that are not currently placed in the active scene.

After the read-only model is strong enough, later milestones can introduce carefully scoped main-thread and safe interaction abstractions without making those higher-level helpers mandatory for loader compatibility.

## Release validation

**v0.0.2 — Data Center Discovery Foundation** was published from commit:

```text
4b6627dd5d99877532e375bf358bccc9770032e7
```

Publication validation:

- **168/168** automated tests passed;
- GitHub Actions run #10 completed successfully on the release commit;
- release ZIP SHA-256 matches GitHub's uploaded asset digest.

## Disclaimer

DCML is an independent community project and is not affiliated with Waseku, Data Center, MelonLoader, Steam, or Valve.
