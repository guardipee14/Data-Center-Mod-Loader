# Package Sources

DCML v0.0.6 introduces a host-neutral package-source boundary.

The package-source abstraction exists above local module-package discovery.
`DCMLPackageDiscovery` continues to validate staged package directories and
their `manifest.json` files. A package source instead describes where package
entries can be discovered before a later adapter stages one into a local
package directory.

## Core contract

`IDCMLPackageSource` exposes:

- a stable `DCMLPackageSourceDescriptor`;
- read-only `DiscoverPackages()`.

A source descriptor contains:

- source ID;
- display name;
- source-defined type identifier;
- advertised capabilities.

Every source must advertise `Discovery`.

The initial capability flags are:

- `Discovery`;
- `Staging`;
- `UpdateMetadata`.

The latter two flags only describe capabilities. They do not add staging or
update methods to the initial core contract and do not authorize DCML to
perform platform actions.

## Opaque package keys

`DCMLPackageSourceEntry.PackageKey` is source-specific and opaque to DCML
Core. A future Workshop adapter may use a Workshop-specific identifier while
another source may use a completely different key format.

Core does not infer filesystem paths, platform ownership, subscription state,
or installation state from a package key.

## Discovery safety

Package-source discovery is read-only.

Discovery must not:

- install or update packages;
- subscribe to Workshop items;
- launch external installers;
- bypass Steam, Boosteroid, cloud-provider, or other platform restrictions;
- silently mutate a player's package configuration.

Source issues use stable codes/messages instead of requiring provider
exception objects or sensitive paths to cross the core boundary.

## Relationship to local discovery

Package sources do not replace `DCMLPackageDiscovery`.

The intended flow is:

1. discover source entries;
2. explicitly choose/plan a package operation;
3. stage through a source adapter that supports staging;
4. run existing local package discovery/manifest validation against the
   staged package;
5. continue through compatibility, dependency resolution, and runtime
   activation.

Only step 1 is part of the initial package-source abstraction.

## Data Center Workshop adapter

The Data Center-specific Workshop source/staging adapter is documented in
`WORKSHOP-STAGING.md`. It discovers only Workshop items already materialized
on disk and stages them through an explicit local copy operation.

It does not subscribe, download, launch Steam, or bypass provider restrictions.

## Package/update metadata

The safe package/update metadata model is documented in
`PACKAGE-UPDATE-METADATA.md`. Sources may opt into metadata reporting without
gaining permission to stage, install, update, subscribe, download, or launch a
platform provider.

## Update/version policy

The pure version-policy evaluator is documented in `UPDATE-VERSION-POLICY.md`.
It classifies version/channel transitions and returns a structured recommendation
without staging or mutating anything.

## Next v0.0.6 work

The next roadmap item is dependency-aware update planning.
