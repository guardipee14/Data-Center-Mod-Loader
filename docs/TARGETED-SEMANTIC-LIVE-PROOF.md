# Targeted Semantic Live Proof

The existing TestModule UI probe intentionally queries only:

```text
user-interface
```

with a maximum of 32 results. It proves the optional Data Center helper API is
available, but it cannot prove the newer physical semantic rules.

This diagnostic adds independent read-only queries for:

- `server`
- `rack`
- `network-device`
- `cable`

Each query:

- is scoped to the initialized scene;
- includes inactive objects;
- excludes unknown entities;
- requests up to 64 semantic results;
- uses the normal `DataCenterEntityDiscovery` path, including its deterministic
  raw-object pagination and optional type-hierarchy support.

The lifecycle proof adds:

```text
TargetedSemanticRuns
LastTargetedSemanticScene
LastTargetedSemanticCounts
LastTargetedSemanticAtLimit
LastTargetedSemanticError
LastTargetedSemanticSample
```

`LastTargetedSemanticAtLimit` lists kinds that returned the full 64-result
diagnostic bound. A kind listed there means "at least 64 were found", not that
the scene contains exactly 64.

This is diagnostic-only. It does not add a loader requirement and does not
modify game state.
