# Dependency-Aware Update Planning

DCML v0.0.6 includes a non-mutating planner that combines installed-version
evidence, source-provided update metadata, and version-policy decisions.

The planner produces an ordered plan or blocking issues. It does not stage,
install, replace, subscribe, download, launch a provider, or otherwise mutate
package state.

## Inputs

`DCMLPackageUpdatePlanner.Plan(...)` accepts:

- installed module/version evidence;
- available `DCMLPackageUpdateMetadata` records;
- one or more requested module IDs;
- optional `DCMLPackageVersionPolicyOptions`.

Installed versions and update metadata are indexed case-insensitively by module
ID. Duplicate installed evidence or duplicate metadata fails closed.

## Required dependency handling

For each requested update, the planner evaluates required dependencies from the
target metadata.

A required dependency is treated as already satisfied when:

- the dependency is installed; and
- either no minimum version is declared or the installed version meets that
  minimum.

Satisfied dependencies are not added to the plan.

When an installed required dependency is too old, the planner may coordinate a
dependency update only when:

1. update metadata exists for that dependency;
2. the metadata target meets the parent's declared minimum version;
3. the dependency's own version-policy decision is not blocked or `NoAction`;
4. the dependency's required dependencies can also be satisfied safely.

## Missing dependencies

A required dependency that is not installed blocks the plan.

This milestone does not reinterpret a missing dependency as authorization to
perform a new installation. Installation/source-selection policy remains a
separate concern.

Optional dependencies do not block update planning.

## Ordering

Planned dependency updates are emitted before their dependents.

Within otherwise equivalent choices, module IDs are evaluated with
case-insensitive deterministic ordering. This follows the same dependency-first
principle used by DCML runtime dependency resolution while operating on update
metadata rather than staged packages.

## Cycles

A cycle among dependency updates that are actually required for the requested
transition blocks the plan with `DCML_UPDATE_PLAN_DEPENDENCY_CYCLE`.

The planner does not guess an order or break the cycle automatically.

## Review-required transitions

If version policy permits a transition only with `ReviewRequired`, the plan may
still be built successfully and `RequiresReview` is set.

That flag is evidence for the caller. It is not approval to execute anything.

## Fail-closed conditions

Planning fails closed for conditions including:

- requested module not installed;
- requested metadata missing;
- duplicate installed-version evidence;
- duplicate update metadata;
- missing required dependency;
- required dependency too old with no update metadata;
- available dependency target below the required minimum;
- dependency update blocked by version policy;
- dependency-update cycle.

A result with issues must not be treated as an executable partial plan.

## Safety boundary

A successful plan means only that the available version/dependency evidence can
be ordered consistently under the selected policy.

It does not mean:

- package bytes have been staged or validated;
- update metadata is an authoritative package manifest;
- a platform action is permitted;
- Steam, Boosteroid, or another provider may be bypassed;
- installation or replacement has been authorized.

## Platform/provider restriction gate

The final v0.0.6 safety gate is documented in `PROVIDER-RESTRICTIONS.md`.

The repository now enforces the sanctioned-only package/provider boundary in CI
and release readiness. A successful dependency plan remains evidence only and
cannot authorize provider bypass, subscription, download, process launch, or
direct network retrieval.

## v0.0.6 feature status

All v0.0.6 feature items are implemented. Release validation and exact-artifact
live proof remain before prerelease publication.
