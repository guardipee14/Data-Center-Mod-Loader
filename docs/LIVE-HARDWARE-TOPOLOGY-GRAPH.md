# Live Hardware Topology Graph

> **Semantic correction:** later live correlation proved that
> `SFPModule.link -> CableLink` is an SFP-module insertion / slot-occupancy
> relationship, not a physical device-to-device network connection. The
> current API therefore emits `sfp-module-insertion` as a structural edge and
> labels the target node `sfp-slot`.

The original identity proof established all 18 live SFP modules had a non-null
`link` reference to `Il2Cpp.CableLink`.

Example:

```text
sfp:SFP_RJ45 -> sfp-slot:SFP_Slot2.003
```

Component instance IDs remain the authoritative identity. Display names are
metadata only.

Subsequent live evidence established:

- 36 `CustomerBase.cableLinks[]` slot components across nine CustomerBases;
- 18 occupied slots and 18 empty slots;
- every topology target was one of the 18 occupied slots;
- occupied slots exposed `insertedSFP` pointing back to the originating
  `SFPModule`;
- both occupied and empty slots had `isSFPPort = true`;
- the observed slot components had `cableIDsOnLink = -1`;
- no `parentServer`, `parentSwitch`, `parentPatchPanel`, `parentInternet`, or
  endpoint reference established a device-to-device network path.

The graph therefore currently contains only:

- live SFPModule scene instances;
- observed SFP-slot `CableLink` components;
- directly observed `sfp-module-insertion` structural edges.

It does **not** emit physical network-connection edges yet.

`DataCenterHardwareTopologyEdge.Kind` classifies edge semantics. Current SFP
insertion edges use:

```text
Relationship = sfp-module-insertion
Kind         = Structural
```

`DataCenterHardwareTopologyGraph.NetworkConnectionEdges` remains empty until a
real cable-chain endpoint contract is proven.

Resource definitions are excluded. Null references do not produce inferred
edges.

`DataCenterApi.Topology` remains an optional recommended helper. It is not
required for DCML loader compatibility.

The graph is built from already-read snapshots. No gameplay methods are
invoked, no writes are made, and no native pointers are exposed.
