# Data Center Component Inventory

The component inventory is a read-only evidence tool in the optional
`DCML.DataCenter` library.

It exists to discover what Data Center actually places in gameplay scenes
before semantic classifications are added.

## Why

DCML should not guess that an internal type represents a rack, server, switch,
cable, machine, or other gameplay concept.

Instead:

1. enter a real scene;
2. inventory object snapshots;
3. observe actual `Il2Cpp.*` and `UnityEngine.*` component types;
4. inspect example hierarchy paths;
5. only then add conservative semantic rules.

## API

```csharp
DataCenterComponentCatalogSnapshot snapshot =
    api.Components.Scan(
        new DataCenterComponentCatalogQuery(
            sceneName: "Gameplay"));
```

The catalog records:

- scanned object count;
- unique component-type count;
- `Il2Cpp.*` type count;
- `UnityEngine.*` type count;
- per-type object count;
- active/inactive object counts;
- bounded example hierarchy paths.

The scan consumes immutable `IDCMLGameObjectDiscovery` snapshots. It does not
expose live Unity objects and does not change game state.

## Probe files

The test module writes one latest inventory file per initialized scene:

```text
UserData\DCML\Data\dcml.test.lifecycle\
    DCML.ComponentInventory.<scene>.log
```

The file is divided into:

- Il2Cpp component types
- UnityEngine component types
- Other component types

Within each section, types are ordered by object count and then name.

## Safety bounds

A scan inspects at most 16,384 object snapshots and stores at most 8 example
hierarchy paths per component type by default.

These limits are intentionally bounded because the inventory is diagnostic
evidence, not a live object database.


## Native IL2CPP type identity

On IL2CPP, `GameObject.GetComponents<Component>()` can return managed wrappers
whose CLR type is only `UnityEngine.Component` even when the native object is a
game-specific component.

The MelonLoader host therefore resolves the component's native IL2CPP class
identity through `Il2CppObjectBase.ObjectClass` and the Il2CppInterop class
name/namespace APIs. Managed `GetType()` is retained only as a fallback.

This lets snapshots preserve names such as `Il2Cpp.SetIP` and other real
Data Center component classes instead of collapsing most scripts to
`UnityEngine.Component`.
