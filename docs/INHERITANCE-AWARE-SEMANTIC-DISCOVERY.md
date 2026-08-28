# Inheritance-Aware Semantic Discovery

The optional `DCML.DataCenter` semantic layer now supports inheritance-aware
component classification and paged filtered discovery.

This remains recommended developer tooling. It is not required for a mod to be
loaded by DCML.

## Assignable-to rules

`DataCenterEntityRule` adds an optional matcher:

```csharp
componentTypeAssignableTo: "Il2Cpp.NetworkSwitch"
```

When `IDCMLGameTypeCatalog` is available, semantic discovery can walk the
loaded type hierarchy.

For example, the observed Data Center runtime hierarchy is:

```text
Il2Cpp.Firewall
    -> Il2Cpp.NetworkSwitch
        -> Il2Cpp.UsableObject

Il2Cpp.Router
    -> Il2Cpp.NetworkSwitch
        -> Il2Cpp.UsableObject
```

A custom rule can therefore classify all current and future subclasses without
listing each exact type.

Exact identity still works even when the type catalog is unavailable.

## Default inherited fallbacks

The default recommended rules keep their exact high-confidence matches and add
lower-priority inheritance-aware fallbacks for:

- `Il2Cpp.NetworkSwitch` -> `network-device`
- `Il2Cpp.Server` -> `server`
- `Il2Cpp.Rack` -> `rack`
- `Il2Cpp.CableLink` -> `cable`

Exact Router, Firewall, NetworkSwitch, Server, Rack, and CableLink rules retain
higher priority, so their stable rule IDs are preserved.

`RackMount` and `SFPModule` remain intentionally unclassified at the top-level
entity-kind layer.

## Optional type catalog

`DataCenterApi.Create(context)` resolves `IDCMLGameTypeCatalog` when the host
provides it.

The catalog is optional. If it is unavailable:

- exact component rules continue to work;
- hierarchy-only matches simply do not match;
- DCML.DataCenter itself still works for the existing exact/UI features.

## Paged semantic search

The complete tested `BaseScene` contains more than one low-level discovery
page.

`DataCenterEntityDiscovery.Find(...)` now requests consecutive deterministic
low-level pages when a filtered semantic query has not yet found enough
results.

This prevents a query such as:

```csharp
new DataCenterEntityQuery(
    kind: DataCenterEntityKinds.Server,
    includeUnknown: false)
```

from silently missing servers that occur after the first 16,384 raw objects.

Scanning stops when:

- the requested semantic result count is reached; or
- the host returns a partial final page.

No game state is modified.


## V2 compile-safety fix

The V2 patch avoids applying the null-conditional operator directly to the
`DataCenterTypeHierarchy.IsAssignableTo` method group. A nullable
`Func<string, string, bool>` local is assigned explicitly only when the
hierarchy service exists.

The hierarchy lookup also explicitly rejects a null `DCMLGameTypeInfo` after
`TryGetType`, satisfying nullable analysis for both `netstandard2.1` and
`net6.0`.
