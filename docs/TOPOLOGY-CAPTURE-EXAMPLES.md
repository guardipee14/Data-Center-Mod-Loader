# Topology Capture Examples

This document and the companion build-checked example project show how a DCML
module can consume the public Data Center topology API without depending on
TestModule internals.

Example project:

```text
examples\DCML.DataCenter.TopologyCapture\
```

The project targets `net6.0` and references:

- `DCML.Core`
- `DCML.DataCenter`
- `DCML.DataCenter.Persistence`

The examples deliberately avoid save discovery, newest-save selection, object
name heuristics, and write behavior.

## Public capture surface

Topology is available through:

```csharp
DataCenterApi api =
    DataCenterApi.Create(
        context);

if (api.Topology is null)
{
    return;
}

DataCenterHardwareTopologyGraph graph =
    await api.Topology.CaptureAsync(
        query);
```

`DataCenterApi.Topology` is optional because it requires the host's component
state capability.

The caller supplies a `DataCenterHardwareSnapshotQuery`. The topology service
uses that query's:

- `SceneName`
- `IncludeSceneObjects`
- `IncludeResources`
- `MaxPerType`

The example project intentionally accepts a caller-created query rather than
inventing a separate query-builder API.

## Live-only topology

Use:

```csharp
DataCenterHardwareTopologyGraph? graph =
    await TopologyCaptureExamples.CaptureLiveAsync(
        context,
        query);
```

This calls `DataCenterApi.Create(context)` with no persistence source.

The result contains live structural relationships such as SFP insertion
relationships observed through the host's read-only component-state service.

## Explicit scene targeting

When a mod intends to capture one scene, it should explicitly construct its
`DataCenterHardwareSnapshotQuery` for that scene and pass it to:

```csharp
DataCenterHardwareTopologyGraph? graph =
    await TopologyCaptureExamples.CaptureExplicitSceneAsync(
        context,
        sceneQuery);
```

The reusable example rejects a query when:

- `IncludeSceneObjects` is false; or
- `SceneName` is empty.

The helper does not guess the active scene or substitute a different scene.

Modules that react to scene lifecycle events should create the query from the
scene they intentionally chose to inspect, after their own scene-readiness
gate has been satisfied.

## Optional persistence-backed topology

Persistence remains opt-in.

A module can include `DataCenterProcessCablePersistenceSettings` inside its own
module-owned settings and then call:

```csharp
DataCenterHardwareTopologyGraph? graph =
    await TopologyCaptureExamples.CaptureWithOptionalPersistenceAsync(
        context,
        query,
        settings.CablePersistence);
```

Internally the example uses:

```csharp
IDataCenterCablePersistenceSource? persistence =
    DataCenterProcessCablePersistenceSourceFactory.Create(
        persistenceSettings);

DataCenterApi api =
    DataCenterApi.Create(
        context,
        persistence);
```

If persistence settings are disabled or incomplete, the factory returns
`null`, and capture remains live-only.

If the settings are enabled and complete, `DataCenterHardwareTopology`
automatically reads the explicitly selected persistence source and combines its
physical cable evidence with the live graph. A mod author should not manually
call `DataCenterPhysicalCableTopology.Combine` after using
`DataCenterApi.Create(context, persistence)` because the capture path already
performs that combination.

## Reading graph edges

The graph already exposes useful filtered edge collections:

```csharp
graph.StructuralEdges
graph.NetworkConnectionEdges
graph.PhysicalCableEdges
graph.ResolvedEdges
graph.UnresolvedEdges
```

Use `NetworkConnectionEdges` when you want every network relationship.

Use `PhysicalCableEdges` when you specifically require physical cable evidence
from persistence.

Example:

```csharp
foreach (
    DataCenterHardwareTopologyEdge edge in
    graph.PhysicalCableEdges)
{
    int? cableId =
        edge.PhysicalCableID;

    string source =
        edge.Source.IdentityKey;

    string target =
        edge.Target.IdentityKey;

    bool sourceResolved =
        edge.SourceResolved;

    bool targetResolved =
        edge.TargetResolved;

    bool fullyResolved =
        edge.IsFullyResolved;

    string evidence =
        edge.EvidenceSource;
}
```

Physical cable edges are `NetworkConnection` edges whose relationship is
`physical-cable-connection`.

They are bidirectional physical links and carry the evidence source associated
with the persisted relationship.

## Evidence-backed filtering

Higher-level mods should prefer explicit evidence fields over inferred
relationships.

For example:

```csharp
IReadOnlyList<DataCenterHardwareTopologyEdge> resolved =
    TopologyCaptureExamples
        .GetFullyResolvedPhysicalCableConnections(
            graph);
```

Or consume the compact evidence DTO:

```csharp
IReadOnlyList<PhysicalCableEvidence> evidence =
    TopologyCaptureExamples.GetPhysicalCableEvidence(
        graph);
```

That DTO exposes:

- physical cable ID;
- source identity key;
- target identity key;
- source-resolution state;
- target-resolution state;
- fully-resolved state;
- bidirectional state;
- evidence source.

Do not infer a physical path from object names, list order, proximity, or the
fact that two runtime objects merely coexist in the same scene.

## Read-only and scene safety

These examples do not mutate game objects or save files.

They:

1. use the existing read-only topology capture API;
2. keep persistence disabled unless explicitly configured;
3. require explicit scene targeting in the scene-specific helper;
4. expose unresolved physical relationships as unresolved rather than filling
   gaps with guesses.

A module should still schedule capture only after the scene state it depends on
is ready. These examples do not bypass the module's existing lifecycle or
main-thread safety rules.
