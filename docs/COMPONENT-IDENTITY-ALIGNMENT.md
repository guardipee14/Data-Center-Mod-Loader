# Component Identity Alignment

Live topology probing established:

- 18 SFPModule.link references existed;
- all referenced values were typed `Il2Cpp.CableLink`;
- a complete scene search scanned 20,836 CableLink component states;
- a complete non-scene/resource search scanned 280 more;
- none matched the relationship IDs when compared to
  `DCMLGameComponentState.InstanceId`.

Inspection of the host reader found the reason.

## Previous identity mismatch

`MelonGameComponentStateReader` read `InstanceId` from the GameObject before
enumerating its components:

```text
GameObject.GetInstanceID()
```

Every component state on that GameObject therefore inherited the GameObject ID.

Relationship values are different. `DCMLGameReference` reads the referenced
Unity object directly:

```text
CableLink.GetInstanceID()
```

That is the component's own Unity instance ID.

The topology graph was therefore comparing:

```text
CableLink component ID
    versus
CableLink GameObject ID
```

Those identities are not expected to match.

## Additive correction

`DCMLGameComponentState` now exposes:

```text
InstanceId           existing GameObject identity; unchanged
GameObjectInstanceId explicit alias for existing identity
ComponentInstanceId  matched component's Unity identity
```

Legacy constructors default `ComponentInstanceId` to `InstanceId`, preserving
existing tests and consumers that construct states manually.

The optional Data Center hardware snapshots carry the same two identities.

## Topology identity rule

Hardware topology now uses `ComponentInstanceId` for:

- SFP topology node identity;
- CableLink topology node identity;
- SFPModule.link target matching;
- paged scene/resource CableLink target lookup.

Display names are still metadata only.

## Safety

This changes identity bookkeeping only.

It does not invoke gameplay methods, mutate Unity objects, expose native
pointers, or change the DCML loader compatibility contract.
