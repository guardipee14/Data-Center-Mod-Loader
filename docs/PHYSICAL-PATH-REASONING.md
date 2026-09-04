# Evidence-Backed Physical Path Reasoning

DCML can now reason across physical cable paths without promoting incomplete or
structural observations into connectivity facts.

The implementation is:

```text
src\DCML.DataCenter\DataCenterPhysicalPathReasoning.cs
src\DCML.DataCenter\Models\DataCenterPhysicalPath.cs
```

## Safety boundary

`DataCenterPhysicalPathReasoning` operates only on an already-captured
`DataCenterHardwareTopologyGraph`.

It does not:

- discover or select save files;
- read game objects;
- mutate game objects;
- mutate save data;
- choose the newest save;
- infer links from object names;
- infer links from list order;
- infer links from physical or scene proximity;
- treat co-presence in a scene as connectivity.

When the graph does not prove a complete route, `FindPath` returns
`Found = false` and no guessed steps.

## Traversable physical evidence

An edge is usable for physical-path traversal only when all of the following
are true:

1. `Kind == NetworkConnection`;
2. `Relationship == physical-cable-connection`;
3. `PhysicalCableID` is present;
4. the cable is bidirectional;
5. `SourceResolved` and `TargetResolved` are both true;
6. both endpoint references have persistent identities;
7. `EvidenceSource` is nonblank.

The current persisted physical-cable source supplies:

```text
Data Center save: NetworkSaveData.cables
```

This lets higher-level mods distinguish an evidence-backed physical hop from a
mere runtime observation.

## Incomplete evidence stays incomplete

Use:

```csharp
IReadOnlyList<DataCenterHardwareTopologyEdge> incomplete =
    DataCenterPhysicalPathReasoning.GetIncompletePhysicalEdges(
        graph);
```

Those edges remain visible for diagnostics, but they do not participate in
route traversal.

An unresolved endpoint therefore creates a real gap in the reasoned path.

DCML does not fill that gap.

## Live structural evidence

Live `SFPModule.link -> CableLink` relationships remain structural
`sfp-module-insertion` evidence.

Use:

```csharp
IReadOnlyList<DataCenterHardwareTopologyEdge> structural =
    DataCenterPhysicalPathReasoning.GetLiveStructuralEvidence(
        graph);
```

This is useful context for a mod, but the reasoner never treats these
structural edges as physical network connections.

That preserves the live-proven distinction already documented by DCML:
SFP-linked `CableLink` objects describe insertion/slot structure, not persisted
end-to-end physical cable connectivity.

## Finding a proven path

Call `FindPath` with topology identity keys:

```csharp
DataCenterPhysicalPathResult path =
    DataCenterPhysicalPathReasoning.FindPath(
        graph,
        source.IdentityKey,
        target.IdentityKey);

if (!path.Found)
{
    // No complete physical route is proven.
    return;
}

foreach (
    DataCenterPhysicalPathStep step in
    path.Steps)
{
    int? cableId =
        step.PhysicalCableID;

    string evidence =
        step.EvidenceSource;

    string from =
        step.FromIdentityKey;

    string to =
        step.ToIdentityKey;
}
```

The search is bidirectional because persisted physical cable edges are
bidirectional.

The returned route is the shortest route available through currently proven
physical edges. It is not a claim about preferred routing, capacity,
redundancy, or logical network policy.

Those higher-level decisions belong to consumers such as future infrastructure
advisors.

## Persisted and live relationship rule

DCML now exposes both categories to reasoning consumers:

- persisted physical cable evidence is eligible for physical traversal when it
  satisfies every proof requirement;
- live SFP insertion evidence is available as structural context but is not a
  traversal edge.

A future bridge between a persisted endpoint and a live object must be added
only when DCML has an explicit identity relationship proving that join.

Until such evidence exists, the two identity spaces remain separate.

This is intentional.

## DC Architect implications

A higher-level advisor can now distinguish:

```text
Proven physical path
    -> every hop has persisted cable evidence

Incomplete physical path
    -> at least one required hop is unresolved or absent

Live structural observation
    -> useful context, but not proof of physical connectivity
```

That makes it possible to build capacity, redundancy, and bottleneck analysis
without silently fabricating cabling relationships.

## Read-only behavior

The reasoning layer is pure analysis over immutable topology models.

It does not alter capture behavior and does not introduce any write path.
