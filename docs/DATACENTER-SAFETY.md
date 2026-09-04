# Data Center Read-Only and Scene Safety

This milestone consolidates the safety rules already proven by DCML's Data Center
integration into a reusable guard for mod authors.

## Existing safety baseline

DCML already has the following boundaries:

- game-object discovery is read-only;
- component-state snapshots are read-only;
- resource and type inspection are read-only;
- the cable persistence source reads an explicitly selected save without writing it;
- physical-path reasoning is pure analysis over captured models;
- SFP insertion remains structural rather than being promoted into physical connectivity;
- the TestModule scene callback does not run heavy diagnostics inline;
- optional automatic diagnostics are delayed, dispatched across update frames, and abandoned when the scene changes.

The new guard does not replace those protections.

## Scene capture guard

Use:

```csharp
DataCenterSceneCaptureSafetyDecision decision =
    DataCenterSceneCaptureSafety.Evaluate(
        context,
        query);

if (!decision.IsAllowed)
{
    return;
}
```

or:

```csharp
if (
    !DataCenterSceneCaptureSafety.CanCapture(
        context,
        query)
)
{
    return;
}
```

The guard is read-only. It never starts capture itself.

## Fail-closed rules

For a query with `IncludeSceneObjects == true`, capture is allowed only when:

1. `IDCMLGameLifecycle` is available;
2. the host reports a current scene;
3. the current scene stage is `Initialized`;
4. if the query names a scene, that name exactly matches the current scene.

If any condition fails, the guard returns `IsAllowed = false`.

This prevents a scene-object query from being started during the earlier
`Loaded` stage or against a stale scene selection.

## Resource-only queries

A query with:

```csharp
IncludeSceneObjects = false
IncludeResources = true
```

does not depend on scene-object readiness and is allowed even when the optional
scene-lifecycle service is unavailable.

This keeps non-scene inspection usable without weakening scene safety.

## Decision reasons

The decision reports one of:

```text
Ready
ResourceOnly
LifecycleUnavailable
NoCurrentScene
SceneNotInitialized
SceneMismatch
```

This lets a mod log why a capture was skipped without guessing.

## Lifecycle callback rule

A lifecycle callback should remain small.

It may:

- record the event;
- update lightweight module state;
- schedule later work.

It should not synchronously launch expensive object discovery, hardware
snapshots, topology capture, or other diagnostic suites while scene
initialization is completing.

The existing TestModule regression gate continues to enforce this rule.

## Deferred work

A mod that schedules work after `Initialized` should re-check the guard
immediately before starting the actual capture.

If the scene has changed in the meantime, the guard returns `SceneMismatch`,
`NoCurrentScene`, or another non-ready result and the stale work should be
abandoned.

Game-object work must continue to respect the existing `IDCMLGameThread`
contract.

## Read-only source gate

The regression suite now also checks both production integration projects:

```text
src\DCML.DataCenter
src\DCML.DataCenter.Persistence
```

for known mutation entry-point markers:

```text
SaveAsync(
WriteAsync(
DeleteAsync(
SetValue(
SetField(
SetMember(
InvokeMethod(
PowerButton(
SetIP(
UpdateAppID(
AddRoute(
RemoveRoute(
AddSubnet(
SetVlanAllowed(
AddRule(
InsertSFP(
RemoveSFP(
```
The gate is intentionally narrow. It does not claim that string scanning can
prove every possible side effect.

Its purpose is to prevent accidental introduction of obvious mutation APIs into
the default Data Center surface. A future write/control feature should be
explicitly designed, reviewed, documented, and accompanied by an intentional
update to this gate.

## What this does not change

This milestone does not:

- modify `DataCenterHardwareSnapshots.CaptureAsync`;
- modify `DataCenterHardwareTopology.CaptureAsync`;
- add save mutation;
- add game-object mutation;
- automatically choose a save;
- make heavy diagnostics synchronous;
- reclassify structural SFP evidence as physical cabling.

The safe capture guard is a reusable precondition for consumers.

Direct calls to the lower-level capture APIs remain possible, so mod authors
must use the lifecycle guard for scene-object work and continue to abandon stale
scheduled work when the scene changes.
