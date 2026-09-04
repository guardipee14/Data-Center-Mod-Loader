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

This milestone validates source identity and package integrity only. Shared-assembly equality and persistence-helper dependency isolation remain separate release-gate milestones.

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

## Dirty working trees

The default is fail-closed:

```text
working tree dirty -> release packaging stops
```

`-AllowDirty` is intentionally explicit and is meant for local packaging tests only. `release-info.json` records that the artifact came from a dirty tree.

A dirty artifact should not be published as an official release.

## Scope of this milestone

This milestone establishes one reproducible repository-owned packaging path.

Separate roadmap items still cover stronger release gates, including:

- automatically validating artifacts against their source commit and SHA-256;
- explicit shared-assembly hash checks;
- persistence-helper dependency-isolation checks;
- the release checklist and live-proof policy.
