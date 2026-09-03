# Game Type Inspection

`IDCMLGameTypeInspector` is a read-only runtime reflection capability for
examining the shape of loaded game/IL2CPP wrapper types.

It complements the existing type catalog:

- `IDCMLGameTypeCatalog` answers **which runtime types exist**.
- `IDCMLGameTypeInspector` answers **what members and contracts a specific
  loaded type exposes**.

The inspector never invokes a reflected field, property, method, or
constructor. It only reads runtime metadata.

## Capability

Hosts that provide the inspector advertise:

```text
dcml.game.type-inspection
```

The service is optional. Mods do not have to use this API to be loadable by
DCML.

## Public API

```csharp
IDCMLGameTypeInspector? inspector =
    context.Services.GetService(
        typeof(IDCMLGameTypeInspector))
    as IDCMLGameTypeInspector;

DCMLGameTypeInspection? server =
    inspector?.Inspect(
        new DCMLGameTypeInspectionQuery(
            "Il2Cpp.Server",
            includeInheritedMembers: true));
```

`Inspect` returns `null` when the requested runtime type is not loaded.

## Inspection snapshot

A `DCMLGameTypeInspection` exposes:

- exact runtime type full name;
- assembly name;
- ordered base-type chain;
- implemented interfaces;
- constructors;
- fields;
- properties;
- methods;
- total member count;
- whether the caller's member limit truncated the returned snapshot.

Each member includes:

- kind;
- name;
- declaring type;
- value/return/property/field type where applicable;
- accessibility;
- static/abstract/inherited flags;
- property read/write flags;
- generic method argument count;
- parameter metadata;
- a deterministic human-readable signature.

Parameters include:

- position;
- name;
- type;
- optional flag;
- `out` flag;
- by-reference flag.

## Inherited members

When `IncludeInheritedMembers` is enabled, the host walks the target's base
class chain and inspects members declared at each level. This makes inherited
game contracts visible without relying on `FlattenHierarchy` behavior.

Constructors remain associated only with the target type because constructors
are not inherited.

## Live diagnostic targets

The current TestModule inspects these evidence-backed Data Center types:

```text
Il2Cpp.Server
Il2Cpp.Rack
Il2Cpp.NetworkSwitch
Il2Cpp.Router
Il2Cpp.Firewall
Il2Cpp.SFPModule
Il2Cpp.CableLink
Il2Cpp.INetworkEndpoint
Il2Cpp.ITimedDevice
```

The detailed report is written to:

```text
UserData\DCML\Data\dcml.test.lifecycle\
  DCML.GameTypeInspection.<scene>.log
```

The lifecycle proof also records summary fields:

```text
GameTypeInspectionRuns
LastGameTypeInspectionScene
LastGameTypeInspectionTypeCount
LastGameTypeInspectionMemberCount
LastGameTypeInspectionAtLimit
LastGameTypeInspectionPath
LastGameTypeInspectionError
LastGameTypeInspectionSummary
```

## Safety boundary

This capability is metadata-only. It does not:

- construct game objects;
- invoke constructors;
- invoke methods;
- get or set field values;
- get or set property values;
- enable or disable objects;
- modify scenes/resources;
- write game state.

Future higher-level Data Center APIs should use this evidence to design
read-only abstractions first. Write/control APIs should only be introduced
after their behavior and safety constraints are understood.
