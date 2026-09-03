# Topology Reference Scope Probe

The paged live-scene CableLink search scanned all 20,836 scene CableLink
components over two pages and found none of the 18 instance IDs referenced by
live SFPModule.link values.

That proves the unresolved edges are not caused by the ordinary 64-item
hardware snapshot window.

This patch probes the remaining target IDs in the non-scene/resource
CableLink scope.

## Classification

A topology edge now distinguishes:

```text
Unknown
SceneObject
NonSceneObject
```

`TargetObserved` means the referenced instance ID was found somewhere.

`TargetResolved` retains the stronger meaning used by the live topology graph:
the target was found as a scene CableLink and may be represented as a live
CableLink node.

A non-scene match is therefore:

```text
TargetObserved = true
TargetResolved = false
TargetLocation = NonSceneObject
```

This prevents loaded helper/resource objects from being silently promoted to
live operating hardware.

## Search behavior

The resource probe:

- runs only for IDs still missing after the complete scene search;
- searches exact `Il2Cpp.CableLink` identity records;
- requests no CableLink member values;
- uses the same bounded paging mechanism;
- stops when every missing target is found or the non-scene set is exhausted.

## Live diagnostics

The test module reports:

```text
LastHardwareTopologyNonSceneSearchPages
LastHardwareTopologyNonSceneCandidatesScanned
LastHardwareTopologyNonSceneTargetMatchCount
LastHardwareTopologyNonSceneSearchExhausted
```

The topology sample also reports:

```text
location=
observed=
resolved=
```

No topology relationship is inferred from a name.


## V2 compile correction

The first patch revision failed the C# compiler because the non-scene target
out variable was declared inside a short-circuited expression:

```text
!sceneResolved && TryGetValue(..., out nonSceneTarget)
```

When `sceneResolved` was true, `TryGetValue` was not evaluated and the compiler
could not prove `nonSceneTarget` was assigned before the later null-coalescing
read.

V2 explicitly initializes the optional reference to null before the lookup.
Runtime semantics are unchanged.

A regression test covers the branch where the scene target resolves and the
non-scene lookup is intentionally skipped.
