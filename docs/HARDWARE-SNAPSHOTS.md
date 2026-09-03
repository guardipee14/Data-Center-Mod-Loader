# Read-only Hardware Snapshots

This milestone adds the first evidence-backed Data Center hardware/network state
API.

## Low-level host-neutral state reader

Capability:

```text
dcml.game.component-state
```

Service:

```text
IDCMLGameComponentStateReader
```

The reader accepts a component type, requested member names, a scene filter and
a source scope (`Scene`, `Resource`, or `Both`).

The MelonLoader host marshals every read through `IDCMLGameThread`, so Unity and
IL2CPP state is inspected on the captured game thread.

Only fields and property getters are read. No gameplay methods are invoked.

Normalized values are intentionally limited to:

- null
- string
- bool
- integer
- floating point / decimal
- enum

Other objects and collections are reported as unsupported rather than silently
traversed.

## Optional Data Center helper

`DataCenterApi.Hardware` is optional and is present only when the host provides
`IDCMLGameComponentStateReader`.

The loader still does not require mods to use `DCML.DataCenter`.

The helper exposes:

- `DataCenterServerSnapshot`
- `DataCenterRackSnapshot`
- `DataCenterNetworkDeviceSnapshot`
- `DataCenterSfpModuleSnapshot`
- `DataCenterCableSnapshot`

### Evidence-backed scalar fields

Server:
`IP`, `ServerID`, `appID`, `currentProcessingSpeed`,
`maxProcessingSpeed`, `isOn`, `isBroken`, `eolTime`, `serverType`.

Rack:
`arePositionTurnedOff`, `targetVolume`.

Network switch:
`PortCount`, `isOn`, `isBroken`, `eolTime`, `switchId`,
`switchType`, `vlanBaselineEstablished`.

Router:
switch state plus `asn`, `nextRouteId`.

Firewall:
switch state plus `clusterIP`.

SFP:
`speed`, `sfpType`, `positionInBox`, `isInTheBox`.

Cable:
`CustomerID`, `cableIDsOnLink`, `connectionSpeed`, `isEndPoint`,
`isFibrePort`, `isSFPPort`, `isStartOrEnd`, `sfpTypeInserted`,
`sfpTypeSupported`, `switchID`, `typeOfLink`.

Collections and object relationships are deliberately deferred for a later
typed model.

## Safety boundary

This API does not call mutation/control methods such as `PowerButton`, `SetIP`,
`UpdateAppID`, `AddRoute`, `RemoveRoute`, `AddSubnet`, `SetVlanAllowed`,
`AddRule`, `InsertSFP`, `RemoveSFP`, or rack occupancy mutators.

## Live proof

The test module captures both scene objects and loaded resources after each
initialized scene and records:

```text
HardwareSnapshotRuns
LastHardwareSnapshotScene
LastHardwareSnapshotServerCount
LastHardwareSnapshotRackCount
LastHardwareSnapshotNetworkDeviceCount
LastHardwareSnapshotSfpCount
LastHardwareSnapshotCableCount
LastHardwareSnapshotError
LastHardwareSnapshotSample
```

The sample includes scalar values, proving that state values are being read
rather than merely rediscovering object types.
