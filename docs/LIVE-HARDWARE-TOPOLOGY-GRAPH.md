# Live Hardware Topology Graph

The live relationship proof established all 18 live SFP modules had a
non-null `link` reference to `Il2Cpp.CableLink`.

Example:

```text
sfp:SFP_RJ45->Il2Cpp.CableLink#426278:SFP_Slot2.003
```

The target display name does not resemble `CableLink`, so names cannot be used
as topology identity.

The graph resolves targets by captured Unity `InstanceId`. `TypeName` is kept
for validation and diagnostics; names are display metadata only.

The first graph contains only:

- live SFPModule scene instances;
- live CableLink scene instances;
- directly observed `sfp-link` edges.

Resource definitions are excluded. Null cable-parent references do not produce
inferred edges.

Each edge exposes:

```text
Relationship
Source
Target
TargetResolved
ResolvedTargetName
```

`DataCenterApi.Topology` is an optional recommended helper. It is not required
for DCML loader compatibility.

The graph is built from already-read snapshots. No gameplay methods are
invoked, no writes are made, and no native pointers are exposed.
