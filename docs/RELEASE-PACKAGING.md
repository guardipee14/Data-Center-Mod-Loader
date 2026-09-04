# Repository-Owned Release Packaging

DCML release packaging is produced by the repository-owned PowerShell script:

```text
tools/Build-DCMLRelease.ps1
```

The script is the canonical local packaging path. A future CI release job should call this script rather than duplicate package-layout logic in workflow YAML.

## Why this exists

Earlier prereleases were assembled successfully, but the exact packaging procedure was not owned by the repository. That makes it too easy for local and CI release layouts to drift.

The release script now owns:

- the release build entry points;
- the `Mods`, `UserLibs`, and module package layout;
- persistence-helper publication and staging;
- staged TestModule manifest versioning;
- ZIP creation;
- SHA-256 generation;
- source-commit and working-tree metadata capture.

This milestone does **not** publish a GitHub release or upload assets automatically.

## Standard release build

From the repository root:

```powershell
.\tools\Build-DCMLRelease.ps1 `
    -Version '0.0.4' `
    -DataCenterRoot 'C:\Program Files (x86)\Steam\steamapps\common\Data Center'
```

By default, the script requires a clean Git working tree. This keeps normal release artifacts tied to a committed source state.

For local packaging validation while the release-script change itself is still uncommitted, use:

```powershell
.\tools\Build-DCMLRelease.ps1 `
    -Version '0.0.4-dev' `
    -DataCenterRoot 'C:\Program Files (x86)\Steam\steamapps\common\Data Center' `
    -AllowDirty
```

Artifacts are written under the ignored directory:

```text
Artifacts\Releases\v<version>\
```

## Outputs

Each run produces:

```text
Artifacts/Releases/v<version>/
  DCML-v<version>.zip
  DCML-v<version>.sha256
  release-info.json
  staging/
  helper-publish/
```

`release-info.json` records:

- requested version;
- Git source commit;
- whether the working tree was clean;
- whether the build was executed;
- ZIP SHA-256;
- packaged file count;
- generation timestamp.

The next release-validation milestone can use this metadata to verify an artifact against its source commit rather than inventing a second metadata format.

## Automatic artifact validation

Every package produced by `Build-DCMLRelease.ps1` is validated before the release builder reports success.

The release builder invokes:

```text
tools/Test-DCMLReleaseArtifact.ps1
```

The validator fails closed unless all of the following agree:

- the expected Git source commit and `release-info.json` `sourceCommit`;
- the version-derived package filename and `release-info.json` `packageFile`;
- the actual ZIP SHA-256;
- `release-info.json` `packageSha256`;
- the hash recorded in `DCML-v<version>.sha256`;
- the package filename recorded in the `.sha256` file.

A normal release build passes the source commit captured at the start of packaging directly into the validator. This prevents a package from reporting success when its metadata points at a different commit.

Standalone validation can also be run explicitly:

```powershell
.\tools\Test-DCMLReleaseArtifact.ps1 -ReleaseDirectory '.\Artifacts\Releases\v0.0.4' -ExpectedSourceCommit '<full Git commit SHA>'
```

If `-ExpectedSourceCommit` is omitted, the validator uses the current repository `HEAD`. For historical artifacts, pass the expected source commit explicitly.

The release gate now validates source identity, outer package integrity, declared shared-assembly equality, and persistence-helper dependency isolation.

## Shared-assembly hash gate

The artifact validator also verifies byte identity for assemblies intentionally duplicated across release package locations.

Current declared pair:

```text
UserLibs/DCML.DataCenter.dll
    ==
UserData/DCML/Modules/DCML.TestModule/DCML.DataCenter.dll
```

Both files must exist exactly once in the final ZIP and their SHA-256 hashes must match.

The comparison is performed against ZIP entries from the final release artifact, not the staging directory. This prevents a package from passing because staging was correct while the actual published archive drifted.

The shared pairs are declared explicitly in `tools/Test-DCMLReleaseArtifact.ps1`. Future intentional shared copies should be added to that declaration rather than inferred from filenames or directory proximity.

A shared-assembly mismatch fails the same artifact-validation gate that already checks the source commit and outer package SHA-256.
## Persistence-helper dependency-isolation gate

The final release ZIP must keep the .NET 8 persistence helper and its private dependency closure under:

```text
UserData/DCML/Modules/DCML.TestModule/PersistenceHelper/
```

The validator requires the known risky runtime dependencies `System.Formats.Nrbf.dll` and `System.Reflection.Metadata.dll` inside that boundary, along with the helper DLL and its `.deps.json` / `.runtimeconfig.json` metadata.

The gate then treats every DLL actually packaged inside `PersistenceHelper/` as helper-private and rejects any same-named assembly found elsewhere in the ZIP, including `Mods/`, `UserLibs/`, or the TestModule root.

This keeps the NRBF dependency closure out of the MelonLoader .NET 6 load context while allowing the out-of-process .NET 8 helper to own those dependencies.

The comparison is performed against the final ZIP, not only staging output.
## Package layout

The ZIP root is the Data Center game root layout:

```text
Mods/
  DCML.Loader.MelonLoader.dll

UserLibs/
  DCML.Core.dll
  DCML.DataCenter.dll

UserData/
  DCML/
    Modules/
      DCML.TestModule/
        DCML.TestModule.dll
        DCML.DataCenter.dll
        DCML.DataCenter.Persistence.dll
        manifest.json
        PersistenceHelper/
          DCML.Persistence.Helper.dll
          DCML.Persistence.Helper.deps.json
          DCML.Persistence.Helper.runtimeconfig.json
          System.Formats.Nrbf.dll
          ...
```

The repository source manifest is copied logically rather than edited in place. The staged package receives the requested release version while `src/DCML.TestModule/manifest.json` remains unchanged.

## Build behavior

A normal run builds:

1. `DCML.Loader.MelonLoader` in Release using the supplied Data Center installation for MelonLoader/Harmony references;
2. `DCML.TestModule` in Release, which also builds its Data Center project references;
3. `DCML.Persistence.Helper` using `dotnet publish` for the framework-dependent .NET 8 helper payload.

The script then stages known required outputs and fails if required release files are missing.

`-SkipBuild` exists for advanced validation from already-built outputs. Normal release production should not use it.

## Release readiness and conditional live proof

After building and validating a release artifact, run the repository-owned readiness gate:

```text
tools/Test-DCMLReleaseReadiness.ps1
```

The readiness gate compares the previous release/base commit with the exact release commit and classifies changed paths.

Automated artifact validation is always required. Live Data Center proof is required only when the changed range contains runtime-facing paths.

Current explicit runtime-facing paths are `src/**` and `tools/Build-DCMLRelease.ps1`. Documentation, tests, examples, workflow files, and the release-validation/readiness tools are non-runtime. Unknown/unclassified paths fail safe as runtime-facing.

When live proof is required, `live-proof.json` must match both the exact release commit and the validated release ZIP SHA-256. This prevents stale proof from another source state or artifact from satisfying the gate.

See [RELEASE-CHECKLIST.md](RELEASE-CHECKLIST.md) for the full release procedure and proof schema.
## Dirty working trees

The default is fail-closed:

```text
working tree dirty -> release packaging stops
```

`-AllowDirty` is intentionally explicit and is meant for local packaging tests only. `release-info.json` records that the artifact came from a dirty tree.

A dirty artifact should not be published as an official release.

## Release-gate status

The repository-owned release path now includes:

- automatic source-commit validation;
- outer ZIP SHA-256 and checksum-file validation;
- explicit shared-assembly hash equality checks;
- persistence-helper dependency-isolation checks;
- release-readiness classification with conditional live Data Center proof.

All v0.0.4 validation and release engineering roadmap items are complete.
