# Data Center Workshop Staging

DCML v0.0.6 includes a Data Center-specific Steam Workshop package source and
staging adapter in the optional `DCML.DataCenter` assembly.

## Scope

The adapter is intentionally limited to content that Steam has already made
available on disk for Data Center.

Data Center's Steam app ID is `4170200`. A normal Steam Workshop content root
therefore ends in:

```text
steamapps\workshop\content\4170200
```

The root is configuration supplied by the host/application. DCML does not scan
Steam libraries globally or assume a specific Steam installation directory.

## Discovery

`DCMLWorkshopPackageSource` implements `IDCMLPackageSource`.

Discovery:

- enumerates immediate child directories of the configured Workshop root;
- accepts numeric Workshop item directory names as opaque package keys;
- does not contact Steam;
- does not subscribe to items;
- does not download missing items;
- does not create a missing Workshop root;
- rejects reparse-point item directories.

A missing root is reported as a package-source issue rather than being treated
as permission to create, download, or repair Steam content.

## Staging

The adapter also implements `IDCMLPackageStagingSource`.

Staging is an explicit local copy operation from one already-materialized
Workshop item directory into a caller-supplied staging root.

The adapter:

1. verifies the entry belongs to the Data Center Workshop source;
2. requires a numeric Workshop item ID;
3. requires the item to exist beneath the configured Workshop root;
4. rejects reparse points while recursively copying;
5. copies into a temporary sibling staging directory;
6. atomically renames that completed temporary directory into the final
   staging directory;
7. refuses to overwrite an existing staging target.

The adapter does not modify the original Workshop item.

## Validation after staging

Workshop staging does not imply that an item is a valid DCML module.

After staging, callers should run the existing local `DCMLPackageDiscovery`
and manifest-validation path against the controlled staging area before any
compatibility, dependency, or runtime activation decision.

This separation keeps Workshop availability and DCML package validity as
different pieces of evidence.

## Platform boundary

The adapter must not be used to bypass Steam or another execution provider.

It contains no:

- Steam subscription operation;
- Workshop download request;
- Steam process invocation;
- Steamworks dependency;
- HTTP client;
- cloud-provider workaround.

If Steam or a cloud provider does not make an item available to the current
environment, DCML reports that the item is unavailable. It does not attempt to
circumvent that restriction.

## Next v0.0.6 work

The next roadmap item is the safe package/update metadata model. Update
metadata will describe available state; it will not by itself authorize an
update or a platform action.
