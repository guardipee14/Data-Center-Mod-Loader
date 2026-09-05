# Update and Version Policy

DCML v0.0.6 includes a pure version-policy evaluator for comparing a current
package version with an available package version.

The policy returns a decision. It does not stage, install, replace, subscribe,
download, or otherwise mutate package state.

## Semantic-version precedence

`DCMLPackageVersionPolicy` reuses `DCMLSemanticVersion`.

Both current and available versions must be valid Semantic Versioning 2.0.0
values.

The evaluator classifies precedence as:

- `Invalid`;
- `Same`;
- `Upgrade`;
- `Downgrade`.

Build metadata does not affect SemVer precedence. Different build metadata on
the same semantic version therefore produces `NoAction`.

## Channel transitions

The decision separately records whether the transition is:

- `StableToStable`;
- `StableToPrerelease`;
- `PrereleaseToPrerelease`;
- `PrereleaseToStable`.

## Recommendations

A decision recommendation is one of:

- `Blocked`;
- `NoAction`;
- `Recommended`;
- `ReviewRequired`.

The safe default policy recommends higher-precedence stable targets, returns no
action for equal precedence, blocks downgrades, and blocks prerelease targets.
Explicitly allowed downgrades or prerelease targets still require review.
A higher-precedence prerelease-to-stable transition is recommended.

## Fail-closed behavior

Invalid current or available version strings produce an `Invalid` transition
and a `Blocked` recommendation with a stable reason code. The evaluator does
not repair, coerce, or guess version values.

## Safety boundary

A `Recommended` result means only that the version transition is acceptable
under the current version policy. It does not mean dependencies are
satisfiable, a package has been staged or validated, or a platform action has
been authorized.

## Next v0.0.6 work

The next roadmap item is dependency-aware update planning. That planner will
combine version-policy decisions with dependency metadata while remaining
non-mutating.
