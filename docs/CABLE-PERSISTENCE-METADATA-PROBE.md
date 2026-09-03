# Cable Persistence Metadata Probe

## Purpose

The topology API now correctly treats:

```text
SFPModule.link -> CableLink
```

as structural SFP-module insertion / slot occupancy rather than a physical
network connection.

The next unresolved question is where Data Center stores the **actual physical
cable-chain endpoints**.

This probe takes a metadata-first approach. It identifies cable-related and
network/save-data-related IL2CPP wrapper types, then inspects their runtime type
metadata without reading object state.

## One-shot behavior

The TestModule gains an independent opt-in setting:

```json
{
  "EnableCablePersistenceMetadataProbe": true,
  "CablePersistenceProbeDelayFrames": 900
}
```

This is separate from:

```json
{
  "EnableAutomaticSceneDiagnostics": false,
  "EnableHeavyAutomaticSceneDiagnostics": false
}
```

The installer explicitly keeps both automatic diagnostic settings disabled.

After an initialized scene remains active for the configured delay, the probe:

1. queries the loaded type catalog for `Cable` types;
2. queries loaded `Save` types and keeps network/cable/device-related matches;
3. explicitly attempts `Il2Cpp.NetworkSaveData` and `Il2Cpp.CableLink`;
4. inspects direct metadata for those types;
5. writes a report containing direct fields, direct properties, and only
   endpoint/persistence-relevant methods;
6. disables its own configuration flag after the one-shot attempt.

If the scene changes before the delay expires, the attempt is canceled and the
flag remains enabled so the next initialized scene can try again.

## Metadata only

The probe uses only:

```text
IDCMLGameTypeCatalog
IDCMLGameTypeInspector
```

It does **not** use:

```text
IDCMLGameComponentStateReader
IDCMLGameObjectDiscovery
DataCenterApi.Hardware
DataCenterApi.Topology
```

It does not invoke reflected members.

It does not call:

```text
CollectPatchPanelChainCables
LoadData
Save
SetUpBase
SetUpApp
```

or any other game method.

## Report

The report is written under:

```text
UserData\DCML\Data\dcml.test.lifecycle\
  DCML.CablePersistenceMetadata.<scene>.log
```

The lifecycle proof records:

```text
CablePersistenceMetadataProbeRuns
LastCablePersistenceMetadataProbeScene
LastCablePersistenceMetadataCandidateTypeCount
LastCablePersistenceMetadataInspectedTypeCount
LastCablePersistenceMetadataRelevantMemberCount
LastCablePersistenceMetadataPath
LastCablePersistenceMetadataCandidateTypes
LastCablePersistenceMetadataRelevantMembers
LastCablePersistenceMetadataError
```

## What counts as useful evidence

We are looking for directly named contracts such as:

```text
CableSaveData
NetworkSaveData
cableID / cableIDs
start / end
source / target
port
parentServer
parentSwitch
parentPatchPanel
parentInternet
link
connection
```

Names alone still do not prove semantics. They identify the smallest next
read-only value probe.

A physical `NetworkConnection` topology edge will not be emitted until both
ends can be supported by observed runtime/save-state evidence.
