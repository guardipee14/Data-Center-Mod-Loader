# Hardware Definitions vs Live Instances

The live hardware snapshot proof established that DCML must distinguish loaded
hardware definitions from live scene instances.

Observed in BaseScene:

- Server: 8 resource definitions, 0 scene instances
- Rack: 1 resource definition, 0 scene instances
- Network devices: 6 resource definitions, 0 scene instances
- SFPModule: 42 scene instances in the bounded snapshot
- CableLink: 64 scene instances in the bounded snapshot

The resource definitions exposed default/template values such as:

- server IP `0.0.0.0`
- server power `false`
- router ASN `0`
- switch port count `0`
- per-model server maximum processing capacity

Those values are useful for describing hardware models but must not be treated
as operating infrastructure state.

The scene SFP and cable objects exposed live scalar state such as SFP speed/type
and cable connection speed/flags.

## API

`DataCenterHardwareSnapshot` now exposes:

```text
Source = SceneInstance | ResourceDefinition
```

`IsResource` remains for compatibility.

`DataCenterHardwareSnapshotSet` now provides:

```text
ServerDefinitions / ServerInstances
RackDefinitions / RackInstances
NetworkDeviceDefinitions / NetworkDeviceInstances
SfpModuleDefinitions / SfpModuleInstances
CableDefinitions / CableInstances
```

The original combined collections remain available.

This is an optional `DCML.DataCenter` helper. It is not a DCML loader
requirement.
