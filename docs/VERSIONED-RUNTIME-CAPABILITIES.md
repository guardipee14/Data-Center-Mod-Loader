# Versioned Runtime Capabilities

DCML v0.0.4 development introduces a versioned capability catalog without
removing or changing the existing `IDCMLRuntimeInfo` capability API.

## Compatibility goal

Existing modules may continue to use:

```csharp
var runtime =
    context.Services.GetService(typeof(IDCMLRuntimeInfo))
        as IDCMLRuntimeInfo;

if (runtime?.HasCapability(
        DCMLRuntimeCapabilities.Events) == true)
{
    // The event service is present.
}
```

The string capability identifiers remain stable and case-insensitive.

Newer modules that need a specific revision of a capability may request the
optional versioned catalog:

```csharp
var capabilities =
    context.Services.GetService(typeof(IDCMLCapabilityCatalog))
        as IDCMLCapabilityCatalog;

if (capabilities?.SupportsCapability(
        DCMLRuntimeCapabilities.Events,
        "1.0.0") == true)
{
    // Event capability API 1.0.0 or newer is available.
}
```

## Capability versions are not DCML release versions

A capability version describes the compatibility contract for one runtime
service. It is independent from the overall DCML release version.

For example:

```text
DCML release:                  0.0.4-development
dcml.events capability API:   1.0.0
```

A later DCML release may keep `dcml.events` at `1.0.0` if that service contract
has not changed incompatibly.

## Current initial capability version

All capabilities already present before the versioned catalog are introduced
into the catalog as:

```text
1.0.0
```

The catalog itself is advertised as:

```text
dcml.runtime-capabilities
```

at version `1.0.0`.

## Minimum-version checks

`SupportsCapability(id, minimumVersion)`:

1. looks up the capability ID case-insensitively;
2. validates both versions using DCML's existing Semantic Versioning 2.0.0
   rules;
3. compares SemVer precedence;
4. returns `true` when the advertised capability version is greater than or
   equal to the requested minimum.

Invalid minimum versions return `false`.

## Fallback behavior

A module that only needs behavior available in the original unversioned
capability contract may fall back to `IDCMLRuntimeInfo.HasCapability(...)`
when `IDCMLCapabilityCatalog` is unavailable.

A module that requires behavior introduced by a newer capability version
should treat an absent `IDCMLCapabilityCatalog` as unsupported rather than
guessing.

This preserves compatibility with older DCML hosts while allowing newer mods
to state precise API requirements.

## Loader acceptance

The capability catalog is an optional developer-facing runtime service.

DCML does **not** require a mod to consume this service, `IDCMLRuntimeInfo`,
`DCML.DataCenter`, or any future SDK layer merely to be considered loadable.
Compatible mods may use lower-level Unity, IL2CPP, Data Center, or host APIs
when appropriate.
