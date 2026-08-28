# Focused IL2CPP Component Inventory

The broad `BaseScene` inventory proved that native IL2CPP component identity
works, but the returned object set reached the 16,384-result ceiling.

Many scene objects are Unity-only rendering or UI objects. They remain useful
to general-purpose discovery, but they should not consume the bounded result
window when the diagnostic goal is to identify Data Center gameplay scripts.

## Low-level prefix filtering

`DCMLGameObjectQuery` now accepts an optional component type prefix:

```csharp
new DCMLGameObjectQuery(
    sceneName: "BaseScene",
    componentTypeNamePrefix: "Il2Cpp.");
```

The MelonLoader host applies this filter before sorting and `MaxResults`.

Exact component matching remains available:

```csharp
componentTypeName: "CableLink"
```

Exact and prefix filters can also be combined.

## Optional Data Center catalog

`DataCenterComponentCatalog` forwards its existing `TypeNamePrefix` value to
the low-level discovery query.

That means:

```csharp
api.Components.Scan(
    new DataCenterComponentCatalogQuery(
        sceneName: "BaseScene",
        typeNamePrefix: "Il2Cpp."));
```

first restricts the object result set to objects containing an `Il2Cpp.*`
component, then catalogs only `Il2Cpp.*` component types from those snapshots.

## Diagnostic output

The TestModule now writes focused evidence separately:

```text
DCML.ComponentInventory.Il2Cpp.<scene>.log
```

Existing broad inventory files remain untouched:

```text
DCML.ComponentInventory.<scene>.log
```

This makes the two evidence sets directly comparable.

## Loader contract

The generic prefix filter lives in `DCML.Core` and its MelonLoader host adapter.

`DCML.DataCenter` remains an optional recommended helper library and is not a
requirement for DCML to load compatible mods.
