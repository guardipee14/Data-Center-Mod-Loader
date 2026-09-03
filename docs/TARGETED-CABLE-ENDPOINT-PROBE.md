# Targeted Cable Endpoint Probe

Component identity alignment live-proved all 18 SFPModule.link references as
real scene CableLink components.

The next question is what those specific CableLink components represent.

Reading every CableLink's gameplay-facing properties would be unnecessarily
expensive because the scene contains more than twenty thousand CableLink
components.

## Exact component filter

`DCMLGameComponentStateQuery` now optionally accepts:

```text
ComponentInstanceIds
```

The MelonLoader host checks this identity immediately after obtaining the
component's Unity instance ID and before:

- creating the typed IL2CPP wrapper;
- reflecting requested members;
- normalizing values.

With no IDs supplied, behavior is unchanged.

## Targeted topology detail

After normal topology identity resolution succeeds, the optional Data Center
topology helper performs one exact-ID CableLink detail read for only the
resolved target components.

Members are limited to already-inspected CableLink state:

```text
CustomerID
cableIDsOnLink
connectionSpeed
isEndPoint
isFibrePort
isSFPPort
isStartOrEnd
sfpTypeInserted
sfpTypeSupported
switchID
typeOfLink
insertedSFP
parentInternet
parentPatchPanel
parentServer
parentSwitch
```

Each topology edge may expose the resulting `TargetCable` snapshot.

## Live proof goals

The test module reports:

```text
LastHardwareTopologyTargetCableDetailRequestedCount
LastHardwareTopologyTargetCableDetailFoundCount
LastHardwareTopologyTargetCableParentServerCount
LastHardwareTopologyTargetCableParentSwitchCount
LastHardwareTopologyTargetCableParentPatchPanelCount
LastHardwareTopologyTargetCableParentInternetCount
LastHardwareTopologyTargetCableInsertedSfpCount
LastHardwareTopologyTargetCableSfpPortCount
LastHardwareTopologyTargetCableEndpointCount
```

The topology sample prints those parent references and endpoint flags.

This allows DCML to observe the actual slot/endpoint structure without
invoking CableLink methods or inferring topology from names.


## V2 regression-test correction

The first revision built successfully but four older topology tests failed
because they assumed `CaptureAsync` issued exactly one CableLink query.

With targeted endpoint detail enabled, a successful capture intentionally
performs two distinct operations:

```text
identity search:
    MemberNames.Count == 0

targeted detail read:
    MemberNames.Count > 0
```

The older tests now validate the identity query specifically while allowing
the targeted detail query.

The scene-resolution scope test also proves that the second query remains
scene-scoped and that no resource-scope probe is performed when the scene
identity search succeeds.

Production behavior is unchanged from V1.
