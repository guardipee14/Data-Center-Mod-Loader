# CustomerBase Runtime State Probe

The local subtree proof scanned 639 objects beneath nine exact CustomerBase
roots and found only seven native type families:

```text
Il2Cpp.CableLink
Il2Cpp.CustomerBase
Il2Cpp.CustomerBaseDoor
Il2Cpp.EPOOutline.Outlinable
Il2Cpp.EPOOutline.TargetStateListener
Il2Cpp.SFPModule
Il2Cpp.WorldCanvasCuller
```

No NetworkSwitch, Router, Firewall, Server, PatchPanel, Internet, or Rack
component exists in those local subtrees.

The next evidence source is therefore the `Il2Cpp.CustomerBase` component
itself.

## Exact GameObject filter

`DCMLGameComponentStateQuery` now accepts optional:

```text
GameObjectInstanceIds
```

This is separate from `ComponentInstanceIds`.

The MelonLoader host checks the GameObject identity before building its
hierarchy path or enumerating components. Existing unfiltered behavior is
unchanged.

## Metadata-first state probe

The test module asks `IDCMLGameTypeInspector` for `Il2Cpp.CustomerBase` with
inherited members disabled.

It selects only:

```text
direct
instance
non-static
fields
```

No arbitrary CustomerBase property getters or gameplay methods are invoked by
this probe.

The field names are then passed to `IDCMLGameComponentStateReader` for only
the nine exact CustomerBase GameObjects.

## Value normalization

The existing component-state reader records:

- null;
- string;
- boolean;
- integer;
- number;
- enum;
- Unity object reference;
- unsupported complex value;
- unavailable/error.

An unsupported complex value is still useful because its runtime type name is
recorded. This can reveal save-data, configuration, resource-definition,
manager, or other model types without invoking them.

## Live proof output

The lifecycle proof includes:

```text
LastCustomerBaseStateProbeComponentCount
LastCustomerBaseStateProbeFieldCount
LastCustomerBaseStateProbeValueCount
LastCustomerBaseStateProbeReferenceCount
LastCustomerBaseStateProbeScalarCount
LastCustomerBaseStateProbeNullCount
LastCustomerBaseStateProbeUnsupportedCount
LastCustomerBaseStateProbeUnavailableCount
LastCustomerBaseStateProbeFields
LastCustomerBaseStateProbeReferenceTypes
LastCustomerBaseStateProbeUnsupportedTypes
LastCustomerBaseStateProbeSample
LastCustomerBaseStateProbeError
```

`Il2Cpp.CustomerBase` is also added to the normal detailed game-type
inspection log.

## Safety

This remains read-only.

No fields are written, no gameplay methods are invoked, no new native objects
are created, and no native pointers are exposed.
