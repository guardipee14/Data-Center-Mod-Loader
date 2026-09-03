# Topology Semantic Correction

## Why this correction exists

The first live topology graph treated this observed reference as a generic
`sfp-link` edge:

```text
SFPModule.link -> Il2Cpp.CableLink
```

At that stage the component identity was proven, but the gameplay meaning of
the relationship was not.

Later read-only probes supplied enough evidence to classify it correctly.

## Evidence

Nine live CustomerBase components exposed 36 entries through
`CustomerBase.cableLinks[]`.

The 36 CableLink components split into two equal groups:

```text
18 occupied SFP slots
18 empty SFP slots
```

For all 18 CableLinks referenced by `SFPModule.link`:

```text
isSFPPort     = true
isStartOrEnd  = true
insertedSFP   = the originating SFPModule
```

For the 18 non-target CableLinks:

```text
isSFPPort     = true
insertedSFP   = null
```

The occupied and empty components are therefore SFP slot/link components. The
reciprocal `SFPModule.link` / `CableLink.insertedSFP` state is evidence of
module insertion or slot occupancy.

All 36 observed CustomerBase SFP slots reported:

```text
cableIDsOnLink = -1
```

The targeted slots also did not expose a resolved physical endpoint through:

```text
parentServer
parentSwitch
parentPatchPanel
parentInternet
isEndPoint
```

This means the relationship must not be promoted to a physical network edge.

## API correction

`DataCenterHardwareTopologyEdge` now exposes:

```csharp
DataCenterHardwareTopologyEdgeKind Kind
bool IsNetworkConnection
```

The additive semantic enum is:

```text
Unknown           = 0
Structural        = 1
NetworkConnection = 2
```

Current SFP insertion edges are emitted as:

```text
Relationship = sfp-module-insertion
Kind         = Structural
```

The target node kind is:

```text
sfp-slot
```

instead of the misleading generic `cable`.

The graph also exposes:

```csharp
StructuralEdges
NetworkConnectionEdges
```

No `NetworkConnection` edge is emitted by this patch.

## Compatibility

The new `kind` constructor parameter is optional and defaults to `Unknown`.
Existing consumers that construct `DataCenterHardwareTopologyEdge` directly
continue to compile.

The old `sfp-link` relationship string is no longer emitted by the current
Data Center topology builder because its semantics were misleading.

DCML still does not require mods to use the optional topology API.

## Next evidence target

Physical network topology remains unresolved.

The next investigation should correlate actual saved/runtime cable-chain IDs
and endpoint ownership without invoking mutating gameplay methods. A physical
connection will only be emitted after both ends can be supported by observed
game state.

## Safety

This patch changes topology interpretation only.

It does not:

- call cable-chain traversal gameplay methods;
- mutate CableLink, SFPModule, server, switch, or save state;
- create or destroy Unity objects;
- expose native pointers;
- alter loader acceptance requirements.
