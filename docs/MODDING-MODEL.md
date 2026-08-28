# DCML Modding Model

DCML has two separate responsibilities:

1. **Load compatible mods into Data Center.**
2. **Optionally make mod development easier.**

Those responsibilities must not be confused.

## Loader contract

A mod does **not** have to use `DCML.DataCenter`, the recommended semantic API,
or any other convenience helper merely because DCML is the loader.

The loader is responsible for discovery, validation, dependency ordering,
activation, lifecycle, and host bridging. Compatible mods may use lower-level
Unity, IL2CPP, MelonLoader, DCML Core services, or other supported techniques
when appropriate.

## Recommended development path

`DCML.DataCenter` is an optional companion library intended to reduce repeated
reverse engineering and boilerplate for common Data Center concepts.

A mod may opt in:

```csharp
DataCenterApi api =
    DataCenterApi.Create(context);

var ui =
    api.Entities.Find(
        new DataCenterEntityQuery(
            kind:
                DataCenterEntityKinds.UserInterface));
```

The same mod may still use `IDCMLGameObjectDiscovery` directly or interact with
lower-level game APIs where needed.

## Dependency direction

The dependency direction is intentionally one-way:

```text
DCML.Loader.MelonLoader --> DCML.Core

DCML.DataCenter --------> DCML.Core

A mod may reference:
  DCML.Core only
  DCML.DataCenter + DCML.Core
  lower-level host/game APIs
  or a mixture
```

`DCML.Loader.MelonLoader` must not reference `DCML.DataCenter`.

This makes the semantic layer **recommended, not required**.

## Semantic classification

The semantic API translates low-level object snapshots into convenient entity
kinds. Classifications must be based on observed game evidence and should remain
conservative.

The first built-in rules identify Unity UI objects. Stable identifiers for
future Data Center concepts such as racks, servers, network devices, cables,
and machines are reserved, but their default classification rules should only
be added after their real in-game IL2CPP/component signatures have been
observed and live-proven.

Mods can continue using low-level object/component discovery even when a
semantic helper exists.
