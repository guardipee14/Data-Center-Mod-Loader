# DCML Capability API Reference

This document describes the current versioned runtime-capability surface for
DCML v0.0.4.

Capability requirements are separate from loader acceptance. A module does not
need to request a capability merely to be discovered, validated, resolved,
activated, initialized, started, or stopped. A capability participates in
compatibility only when a package explicitly declares it in
`requiredCapabilities`.

## Stability terminology

DCML distinguishes between:

- **Capability version**: the semantic version advertised by the active host.
- **API stability status**: whether DCML promises long-term compatibility for
  the public contract associated with that capability.

All capabilities currently advertised by the MelonLoader host use capability
version `1.0.0`.

Capability version `1.0.0` does not by itself declare the related API stable.

For v0.0.4:

- all capabilities below are active and versioned;
- all remain **Provisional**;
- no capability API is yet declared **Stable**.

Later roadmap work explicitly covers stabilization of public loader/runtime
contracts and the versioned capability policy.

## Capability table

| Capability ID | Version | Core service contract | Status |
| --- | --- | --- | --- |
| `dcml.logging` | `1.0.0` | `IDCMLLogger` | Provisional |
| `dcml.runtime-information` | `1.0.0` | `IDCMLRuntimeInfo` | Provisional |
| `dcml.runtime-capabilities` | `1.0.0` | `IDCMLCapabilityCatalog` | Provisional |
| `dcml.configuration` | `1.0.0` | `IDCMLConfiguration` | Provisional |
| `dcml.events` | `1.0.0` | `IDCMLEventBus` | Provisional |
| `dcml.game.scene-lifecycle` | `1.0.0` | `IDCMLGameLifecycle` | Provisional |
| `dcml.game.object-discovery` | `1.0.0` | `IDCMLGameObjectDiscovery` | Provisional |
| `dcml.game.type-catalog` | `1.0.0` | `IDCMLGameTypeCatalog` | Provisional |
| `dcml.game.resource-discovery` | `1.0.0` | `IDCMLGameResourceDiscovery` | Provisional |
| `dcml.game.type-inspection` | `1.0.0` | `IDCMLGameTypeInspector` | Provisional |
| `dcml.game.main-thread` | `1.0.0` | `IDCMLGameThread` | Provisional |
| `dcml.game.component-state` | `1.0.0` | `IDCMLGameComponentStateReader` | Provisional |

## Manifest requirements

Declare only capabilities that are mandatory for correct module behavior.

```json
{
  "requiredCapabilities": [
    {
      "id": "dcml.events",
      "minimumVersion": "1.0.0"
    }
  ]
}
```

When a capability is optional, omit it from `requiredCapabilities`, attempt to
resolve its service at runtime, and degrade cleanly if it is unavailable.

Direct Core access remains supported:

```csharp
IDCMLLogger? logger =
    context.Services.GetService(
        typeof(IDCMLLogger))
    as IDCMLLogger;
```

Modules that opt into `DCML.SDK` may use the convenience layer:

```csharp
IDCMLLogger? logger =
    context.GetOptionalService<IDCMLLogger>();
```

Neither lookup style changes package compatibility by itself.

## Capability details

### `dcml.logging`

- Service: `IDCMLLogger`
- Purpose: host-integrated debug, information, warning, and error logging.
- Status: Provisional.

### `dcml.runtime-information`

- Service: `IDCMLRuntimeInfo`
- Purpose: module ID, DCML version, host information, game information, and
  capability visibility.
- Status: Provisional.

### `dcml.runtime-capabilities`

- Service: `IDCMLCapabilityCatalog`
- Purpose: enumerate versioned capabilities and query capability/version
  support.
- Status: Provisional.

### `dcml.configuration`

- Service: `IDCMLConfiguration`
- Purpose: module-specific configuration load/save/delete operations.
- Status: Provisional.

### `dcml.events`

- Service: `IDCMLEventBus`
- Purpose: typed in-process publication/subscription and cross-module
  coordination.
- Status: Provisional.

### `dcml.game.scene-lifecycle`

- Service: `IDCMLGameLifecycle`
- Purpose: current scene identity and lifecycle-stage visibility.
- Status: Provisional.

### `dcml.game.object-discovery`

- Service: `IDCMLGameObjectDiscovery`
- Purpose: read-only game-object discovery.
- Status: Provisional.

### `dcml.game.type-catalog`

- Service: `IDCMLGameTypeCatalog`
- Purpose: game/runtime type catalog queries.
- Status: Provisional.

### `dcml.game.resource-discovery`

- Service: `IDCMLGameResourceDiscovery`
- Purpose: read-only resource discovery.
- Status: Provisional.

### `dcml.game.type-inspection`

- Service: `IDCMLGameTypeInspector`
- Purpose: inspect selected game/runtime types through the host abstraction.
- Status: Provisional.

### `dcml.game.main-thread`

- Service: `IDCMLGameThread`
- Purpose: main-thread detection, posting, and invocation.
- Status: Provisional.

### `dcml.game.component-state`

- Service: `IDCMLGameComponentStateReader`
- Purpose: read selected component-member state through the host abstraction.
- Status: Provisional.

## Host behavior

The current MelonLoader host advertises all twelve capability IDs above at
version `1.0.0` and registers their corresponding Core service contracts
through `IDCMLModuleContext.Services`.

Future host adapters may advertise a different subset. Modules must treat the
active capability catalog as authoritative.

## Optional-layer relationships

`DCML.SDK` is optional and provides convenience access to the same Core service
contracts. It defines no separate capability namespace and is not required for
loader acceptance.

`DCML.DataCenter` is also optional. It is a Data Center-specific semantic layer
built on selected Core game capabilities.

## Current stability summary

```text
Capability version:
    1.0.0 for all current MelonLoader-host capabilities

Stable capability APIs:
    none declared yet

Provisional capability APIs:
    dcml.logging
    dcml.runtime-information
    dcml.runtime-capabilities
    dcml.configuration
    dcml.events
    dcml.game.scene-lifecycle
    dcml.game.object-discovery
    dcml.game.type-catalog
    dcml.game.resource-discovery
    dcml.game.type-inspection
    dcml.game.main-thread
    dcml.game.component-state
```

Stability status must be updated deliberately when later roadmap work promotes
individual contracts or capability-policy guarantees.
