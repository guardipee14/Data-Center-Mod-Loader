# DCML Release Checklist

This checklist is the repository-owned release-readiness policy for DCML.

The release process always requires automated validation. **Live proof is required only when runtime-facing changes are present.**

## 1. Start from a release commit

Before producing an official artifact:

- commit all intended source changes;
- confirm the working tree is clean;
- identify the previous published release commit or tag as the base;
- identify the exact commit being released.

Example:

```powershell
Set-Location 'C:\Dev\DCML'

git status --short

$baseCommit =
    (git rev-list -n 1 v0.0.3).Trim()

$releaseCommit =
    (git rev-parse HEAD).Trim()
```

Do not publish an official release from `-AllowDirty` output. That switch exists only for local release-engineering proof.

## 2. Run the automated test baseline

```powershell
dotnet test .\DCML.sln `
    --configuration Release
```

The expected development baseline for this milestone is:

```text
442 passed
0 failed
0 skipped
```

## 3. Build the repository-owned release artifact

```powershell
$release =
    .\tools\Build-DCMLRelease.ps1 `
        -Version '0.0.4' `
        -DataCenterRoot 'C:\Program Files (x86)\Steam\steamapps\common\Data Center'
```

`Build-DCMLRelease.ps1` already validates:

- source-commit identity;
- package/version naming;
- outer ZIP SHA-256 and checksum agreement;
- declared shared-assembly equality;
- persistence-helper dependency isolation.

## 4. Run release readiness

```powershell
$readiness =
    .\tools\Test-DCMLReleaseReadiness.ps1 `
        -ReleaseDirectory $release.ReleaseDirectory `
        -BaseCommit $baseCommit `
        -ReleaseCommit $releaseCommit

$readiness |
    Format-List
```

The readiness gate compares the base and release commits, classifies the changed repository paths, and decides whether a live Data Center proof is required.

## Change classification

### Runtime-facing

Live proof is required when any changed path is runtime-facing.

Current explicit runtime-facing rules:

```text
src/**
tools/Build-DCMLRelease.ps1
```

`tools/Build-DCMLRelease.ps1` is runtime-facing because changing package layout or staged binaries can change what Data Center/MelonLoader actually loads.

Any path not covered by an explicit non-runtime rule is treated as runtime-facing by default. This is intentional fail-closed behavior.

### Non-runtime

These changes do not, by themselves, require a live game launch:

```text
docs/**
tests/**
examples/**
.github/**
README.md
LICENSE
LICENSE.md
.gitignore
DCML.sln
tools/Test-DCMLReleaseArtifact.ps1
tools/Test-DCMLReleaseReadiness.ps1
```

They still require automated tests and artifact validation.

If a nominally non-runtime file is used to change what is actually staged or loaded at runtime, update the classification rule before release rather than relying on the old category.

## 5. Live proof when required

When `LiveProofRequired` is `True`, install and run the **exact validated release artifact** in Data Center using the intended host.

Do not reuse proof from another commit or another ZIP.

After the live run passes, create:

```text
<release directory>/live-proof.json
```

with this schema:

```json
{
  "schemaVersion": 1,
  "sourceCommit": "<exact full release commit>",
  "packageSha256": "<exact validated release ZIP SHA-256>",
  "result": "passed",
  "game": "Data Center",
  "host": "MelonLoader 0.7.3",
  "observedAtUtc": "2026-09-04T15:00:00Z",
  "summary": "Loader initialized, packaged modules activated, required runtime-facing behavior was exercised, and no DCML runtime initialization error was observed."
}
```

Record `result: passed` only after the live proof actually succeeds.

The readiness gate requires:

- schema version `1`;
- exact `sourceCommit` match;
- exact validated `packageSha256` match;
- `result` equal to `passed`;
- `game` equal to `Data Center`;
- a non-empty host;
- a valid ISO-8601 `observedAtUtc`;
- a non-empty evidence summary.

Then rerun:

```powershell
$readiness =
    .\tools\Test-DCMLReleaseReadiness.ps1 `
        -ReleaseDirectory $release.ReleaseDirectory `
        -BaseCommit $baseCommit `
        -ReleaseCommit $releaseCommit `
        -LiveProofPath (Join-Path $release.ReleaseDirectory 'live-proof.json')
```

Expected runtime-facing result:

```text
Success                  : True
ArtifactValidated        : True
LiveProofRequired        : True
LiveProofProvided        : True
LiveProofValidated       : True
```

## 6. Non-runtime release result

When the changed range contains only non-runtime files, no `live-proof.json` is required.

Expected result:

```text
Success                  : True
ArtifactValidated        : True
RuntimeFacingChangeCount : 0
LiveProofRequired        : False
LiveProofProvided        : False
LiveProofValidated       : False
```

`LiveProofValidated : False` is correct in this case because no live proof was necessary or claimed.

## 7. Final publish checks

Before publishing:

- working tree is clean;
- automated test baseline passes;
- release ZIP validation passes;
- readiness gate returns `Success : True`;
- if `LiveProofRequired : True`, the exact release commit and package SHA-256 have passed live proof;
- release notes state what was automated and, when applicable, what was proven live;
- ZIP and `.sha256` are published together;
- no proprietary Data Center, MelonLoader, or other third-party binaries are committed to the repository.

## Policy intent

The purpose of this gate is to avoid both failure modes:

1. requiring repetitive manual game launches for docs/tests/release-policy-only work that cannot change runtime behavior;
2. publishing runtime-facing changes based only on source tests without proving the exact release package in Data Center.

The gate therefore keeps automated validation mandatory for every release and adds live Data Center proof only when the release diff crosses a runtime-facing boundary.
