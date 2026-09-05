# Platform and Provider Restrictions

DCML v0.0.6 treats platform/provider restrictions as a release invariant.

Package discovery, Workshop staging, update metadata, version policy, and
dependency-aware planning must not become a mechanism for bypassing Steam,
Boosteroid, another cloud provider, or another package/provider boundary.

## Sanctioned-only rule

DCML may operate on package content that the current platform/provider has
already made available to the running environment.

DCML does not interpret package metadata, a version recommendation, or a
successful dependency plan as permission to obtain content through another
channel.

For the Data Center Steam Workshop adapter this means:

- discover only already-materialized Workshop item directories;
- stage only through local filesystem copy;
- do not subscribe to Workshop items;
- do not request Workshop downloads;
- do not invoke Steam;
- do not call Steamworks/Steam UGC APIs;
- do not use direct HTTP/HTTPS retrieval as a substitute;
- do not work around Boosteroid or another cloud provider's restrictions.

## Repository validator

`tools/Test-DCMLProviderRestrictions.ps1` scans the host-neutral Core and the
Data Center package-source adapter for direct provider-access mechanisms.

The validator fails closed when it finds patterns representing:

- process launching;
- direct HTTP/web clients or remote HTTP/HTTPS endpoints;
- Steamworks/Steam API/Steam UGC access;
- Workshop subscribe/unsubscribe/download calls;
- direct native `steam_api` loading.

The validator also fails if an expected source scope is missing or if no source
files are found.

This is intentionally conservative. A future feature that genuinely requires a
new provider integration must introduce a reviewed, explicit provider adapter
and update this policy rather than silently weakening or bypassing the gate.

## CI enforcement

`.github/workflows/ci.yml` runs the provider-restriction validator on pushes
and pull requests.

A policy violation therefore fails CI before a release is prepared.

## Release enforcement

`tools/Test-DCMLReleaseReadiness.ps1` also runs the same validator
unconditionally.

Release readiness cannot return success unless:

```text
ProviderRestrictionsValidated : True
```

This check is independent from live Data Center proof. Live proof verifies
runtime behavior of the exact release artifact when required; the provider
restriction gate verifies that the package/update source surface has not
introduced forbidden provider-access mechanisms.

## Mutation boundary

The gate does not ban all filesystem mutation. The explicit Workshop staging
adapter is allowed to copy an already-materialized item into a caller-controlled
staging directory and to manage its temporary staging directory.

That local staging operation does not grant permission to acquire missing
content or to overwrite provider-managed Workshop content.

Metadata, version policy, and update planning remain non-mutating.

## v0.0.6 status

With this gate in place, all v0.0.6 Package Sources & Workshop Staging feature
items are implemented.

The next work for v0.0.6 is release validation, exact-artifact live proof for
the runtime-facing changes, and prerelease publication. v0.1.0 API
stabilization begins only after v0.0.6 is closed.
