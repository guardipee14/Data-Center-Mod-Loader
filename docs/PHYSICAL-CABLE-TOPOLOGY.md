# Physical Cable Topology

## Status

DCML now has an evidence-backed model for Data Center's persisted physical
cables.

This does **not** reinterpret the existing `SFPModule.link -> CableLink`
relationship. That relationship remains structural SFP insertion / slot
occupancy.

Physical network connections come from a different evidence source:

```text
NetworkSaveData.cables : List<CableSaveData>

CableSaveData
  cableID
  startPoint : CableEndpointSaveData
  endPoint   : CableEndpointSaveData
```

Each saved endpoint carries:

```text
serverID
switchID
customerID
position
type
```

## Live-save evidence

The recovered healthy save produced:

```text
CableSaveDataCount         686
CableEndpointSaveDataCount 1372
DuplicateCableIDGroupCount 0
ResolvedEndpointCount      1372
UnresolvedEndpointCount    0
```

Observed endpoint type correlation:

```text
Type 1 -> serverID   -> Server
Type 2 -> switchID   -> Switch / Router / Firewall / PatchPanel / PatchPanelPort
Type 3 -> customerID -> CustomerBase
```

Important semantic rule:

A Type-2 endpoint can also contain a `serverID`. That field is retained as raw
evidence but does not override the Type-2 `switchID` identity. Real save data
showed Type-2 rows with stale/unmatched server IDs while their network-side
identifier resolved cleanly.

Patch-panel ports are stored in the `switchID` field using identities such as:

```text
PatchPanel_-88216_1
PatchPanel_-88216_2
PatchPanel_-88216_3
```

These resolve to the saved parent patch panel plus the per-port persistent ID.

Type-3 endpoints in the tested healthy save used customer IDs 0 through 9 and
connected to saved router endpoints. DCML therefore labels the persisted
endpoint `CustomerBase`; it does not claim a more specific Internet semantic
without additional evidence.

## Identity model

Runtime hardware still uses Unity/IL2CPP integer instance IDs.

Persisted cable endpoints do not.

`DataCenterHardwareReference` and `DataCenterHardwareTopologyNode` can now
carry an optional persistent identity:

```text
PersistentID
IdentityKind
IdentityKey
HasRuntimeInstance
HasPersistentIdentity
```

Persistence-only references use:

```text
InstanceId = 0
```

rather than fabricating a Unity instance ID.

## Physical cable edge

Each `CableSaveData` becomes exactly one edge:

```text
Relationship     physical-cable-connection
Kind             NetworkConnection
PhysicalCableID  actual cableID
IsBidirectional  true
EvidenceSource   Data Center save: NetworkSaveData.cables
```

The serialized start/end ordering is preserved as source/target for
reproducibility, but the physical network relationship is explicitly marked
bidirectional. It must not be interpreted as packet direction.

Parallel cables between the same endpoint identities remain separate edges
because each edge carries its own `PhysicalCableID`.

## API

Build a physical graph from explicitly supplied save evidence:

```csharp
DataCenterHardwareTopologyGraph physical =
    DataCenterPhysicalCableTopology.Build(
        cables,
        persistenceIndex);
```

Combine it with an existing live structural graph:

```csharp
DataCenterHardwareTopologyGraph combined =
    DataCenterPhysicalCableTopology.Combine(
        liveGraph,
        cables,
        persistenceIndex);
```

The existing live `CaptureAsync` path intentionally does not guess which save
file is currently authoritative. A later host/source adapter can supply an
explicit, validated persistence snapshot without changing these topology
semantics.

## Safety

This layer:

- invokes no game method;
- performs no scene-wide scan;
- does not call `CollectPatchPanelChainCables`;
- writes no game state;
- does not modify a save;
- does not guess ownership from names when the persistence index cannot
  resolve an endpoint.

Unresolved persistence evidence remains unresolved.
