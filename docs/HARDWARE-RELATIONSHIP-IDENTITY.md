# Read-only Hardware Relationship Identity

This milestone extends the read-only component-state layer so Unity/IL2CPP
object references can be represented without leaking Unity types into the
public DCML API.

## Low-level reference value

`DCMLGameValueKind.Reference` is additive value `8`.

A reference carries only host-neutral identity:

```text
InstanceId
Name
TypeName
```

The MelonLoader host recognizes existing `UnityEngine.Object` values, reads
their existing instance ID/name/native IL2CPP type, and returns a
`DCMLGameReference`.

No target object is created, invoked, changed, retained as a public Unity
object, or exposed as a native pointer.

## Evidence-backed Data Center relationships

The optional `DCML.DataCenter` helper now reads only the single-object
relationships directly proven by type/member inspection.

### SFPModule

```text
link -> CableLink
```

### CableLink

```text
insertedSFP      -> SFPModule
parentInternet   -> Internet
parentPatchPanel -> PatchPanel
parentServer     -> Server
parentSwitch     -> NetworkSwitch
```

Collections such as `Server.activeLinks`,
`Server.cablelinks`, and `NetworkSwitch.cableLinkSwitchPorts` remain deferred.
They require a separate bounded collection-snapshot design.

## Safety

Relationship reads still run through `IDCMLGameThread`.

This patch does not invoke:

- cable insertion/removal methods;
- server link registration methods;
- network switch cable or VLAN methods;
- SFP insertion/removal methods;
- any gameplay mutation method.

## Live proof

The test module records:

```text
LastHardwareSnapshotSfpLinkedCount
LastHardwareSnapshotCableParentServerCount
LastHardwareSnapshotCableParentSwitchCount
LastHardwareSnapshotCableParentPatchPanelCount
LastHardwareSnapshotCableParentInternetCount
LastHardwareSnapshotCableInsertedSfpCount
LastHardwareRelationshipSample
```

The relationship sample includes type, Unity instance ID and object name, for
example:

```text
sfp:SFP_RJ45->Il2Cpp.CableLink#123:CableLink (48)
cable:CableLink (48)|server=...|switch=...|patchPanel=...|internet=...|sfp=...
```

A null relationship remains `(null)` and is not inferred from names.
