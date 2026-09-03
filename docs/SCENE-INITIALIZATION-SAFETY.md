# Scene Initialization Safety Fix

## Problem

`DCML.TestModule` previously ran the full scene diagnostic suite synchronously
from `DCMLSceneLifecycleStage.Initialized`.

On a large BaseScene that work included:

- game-object discovery;
- semantic discovery;
- a 22,860-object component inventory;
- type catalog and resource discovery;
- type inspection;
- hardware snapshots and follow-on probes.

The game log demonstrated that this could hold scene initialization for tens of
seconds while Data Center's save restoration was still waiting to run.

Because Data Center can auto-save on exit, a save could be written while its
network collections had not been restored. One observed save was structurally
complete but had every `NetworkSaveData` collection serialized empty.

## Fix

### Default behavior

Automatic scene diagnostics are now disabled by default:

```text
EnableAutomaticSceneDiagnostics = false
```

A scene lifecycle callback records the event and returns immediately. It does
not run discovery or hardware scans inline.

### Optional automatic diagnostics

If explicitly enabled in the test-module configuration, diagnostics are:

1. delayed for a configurable number of update frames;
2. scheduled through the game-thread dispatcher;
3. advanced one stage per drain/update;
4. canceled when the active scene changes.

The safe automatic mode performs only:

- bounded object discovery;
- type catalog;
- resource discovery.

The previously expensive diagnostic set is behind the separate explicit flag:

```text
EnableHeavyAutomaticSceneDiagnostics = true
```

It remains disabled by default.

### Scheduler fairness

`DCMLGameThreadDispatcher.Drain()` now snapshots the number of actions that were
queued at the beginning of a drain. An action posted by another action is
therefore left for the next drain/update instead of recursively running in the
same frame.

This is a general protection against self-reposting work monopolizing the game
thread.

## Configuration defaults

```json
{
  "EnableAutomaticSceneDiagnostics": false,
  "SceneDiagnosticDelayFrames": 600,
  "EnableHeavyAutomaticSceneDiagnostics": false
}
```

The installer writes these safe values into the existing TestModule
configuration while preserving all other configuration properties.

## Safety

The patch installer:

- requires Data Center to be closed;
- verifies the expected pre-fix source baseline before overwriting source;
- creates a full current-save snapshot;
- backs up every changed project/game/configuration file;
- builds;
- requires the complete test gate to pass;
- publishes and installs the fixed DCML host/module;
- clears the old proof log;
- rolls project/game/configuration files back on failure.

`DCMLTrace` is not re-enabled by this patch.
