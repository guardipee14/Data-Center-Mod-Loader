# Target Cable Hierarchy Context Probe

The targeted CableLink endpoint probe established that all 18 SFP-linked
CableLinks are scene SFP-port objects:

- 18/18 targeted details found;
- 18/18 `isSFPPort = true`;
- 18/18 `insertedSFP` references point back to the SFP module;
- 0/18 are `isEndPoint`;
- server/switch/patch-panel/Internet parent fields are all null;
- observed `typeOfLink = Base`.

This means those CableLink components are useful slot/link objects but their
direct CableLink parent fields do not identify the surrounding device.

## Generic object-discovery additions

`DCMLGameObjectQuery` now accepts optional exact:

```text
InstanceIds
```

When supplied, the MelonLoader host checks `GameObject.GetInstanceID()` before
building full object information, so only requested GameObjects pay the
component-enumeration cost.

`DCMLGameObjectInfo` now exposes:

```text
ParentInstanceId
```

This is the parent Transform's GameObject instance ID, or null for a root.

Both additions are optional and backward compatible.

## Probe behavior

For only the resolved target CableLink GameObjects, the test module:

1. requests their exact GameObject IDs;
2. records each object's native component type names;
3. follows `ParentInstanceId`;
4. repeats for at most eight levels;
5. records exact native component types on ancestors.

It counts target chains containing exact:

```text
Il2Cpp.Server
Il2Cpp.NetworkSwitch
Il2Cpp.Router
Il2Cpp.Firewall
Il2Cpp.PatchPanel
Il2Cpp.Internet
Il2Cpp.Rack
```

Router and Firewall count under the network-device ancestry metric.

## Interpretation

A hierarchy ancestor is evidence of scene structure only.

DCML does not automatically convert an ancestor relationship into a physical
or logical network-topology edge.

## Safety

The probe is read-only and uses exact IDs at every ancestry level.
