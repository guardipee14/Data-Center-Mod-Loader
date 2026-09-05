# Package and Update Metadata

DCML v0.0.6 defines a safe, host-neutral metadata model for describing package
versions that a package source can observe.

Metadata is evidence. It is not authorization to perform an update.

## Optional source capability

A source that can provide trustworthy update metadata may implement
`IDCMLPackageUpdateMetadataSource` and advertise the existing
`UpdateMetadata` capability.

Sources that cannot provide trustworthy metadata should not advertise that
capability.

The Data Center Workshop staging adapter does not currently advertise
`UpdateMetadata`.

## Metadata model

`DCMLPackageUpdateMetadata` contains:

- source ID;
- opaque package key;
- stable module ID;
- available package version;
- optional minimum DCML version;
- whether that package version requires restart;
- zero or more dependency requirements.

Package versions, minimum DCML versions, and dependency minimum versions use
the same Semantic Versioning 2.0.0 validation already used by DCML manifests.

Dependency metadata contains:

- stable dependency module ID;
- optional minimum acceptable version;
- optional/required status.

Duplicate dependency IDs are rejected case-insensitively.

## Retrieval result

`DCMLPackageUpdateMetadataResult` represents either:

- successful metadata retrieval; or
- a stable error code/message with no metadata.

Provider-specific exception objects, credentials, URLs, commands, staging
paths, and platform actions are not part of the model.

## Safety boundary

Update metadata must not be interpreted as permission to:

- stage a package;
- install a package;
- replace an installed package;
- subscribe to Workshop content;
- request a Workshop download;
- launch Steam or another provider;
- bypass a platform/provider restriction.

The model intentionally has no automatic-update flag and no executable action.

## Relationship to manifests

A source may describe a package before the package has been staged and
validated as a DCML module. Therefore source-provided update metadata remains
separate from `DCMLModuleManifest`.

After staging, the actual package manifest remains authoritative for package
validation and runtime activation.

A planner may later compare trusted source metadata with installed/staged
manifest state, but must not silently treat source metadata as a validated
manifest.

## Update/version policy

The pure version-policy evaluator is documented in `UPDATE-VERSION-POLICY.md`.
Metadata remains descriptive input; policy evaluation produces only a structured
decision and performs no update.

## Next v0.0.6 work

The next roadmap item is dependency-aware update planning.
