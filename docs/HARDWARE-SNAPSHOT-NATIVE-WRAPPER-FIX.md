# Native IL2CPP Wrapper Rebinding Fix

## Problem

The first live `dcml.game.component-state` proof found the correct hardware
objects, but every requested scalar value was returned as unavailable.

Examples:

- 8 Server resources were found
- 1 Rack resource was found
- 6 network-device resources were found
- 42 SFP modules were found
- 64 CableLink objects were found

The type/member catalog had already proven that members such as `Server.IP`,
`Server.isOn`, `NetworkSwitch.PortCount`, `Router.asn`,
`SFPModule.speed`, and `CableLink.connectionSpeed` exist.

The mismatch was caused by Unity component enumeration returning an IL2CPP
component through a base managed wrapper. Native type identity correctly
resolved the component as `Il2Cpp.Server` (or another Data Center type), but
`component.GetType()` could still be a base wrapper that does not declare the
game-specific properties.

## Fix

Before scalar reflection, the MelonLoader host now:

1. resolves the component's native IL2CPP type name;
2. finds the corresponding loaded managed wrapper type;
3. reads the existing native `Pointer`;
4. creates the correct wrapper using its `IntPtr` constructor;
5. reads the requested property/field from that correctly typed wrapper.

No native object is created or copied. The new wrapper refers to the exact same
existing IL2CPP object pointer.

If rebinding cannot be performed, the reader falls back to the original wrapper
and preserves the existing unavailable-value behavior.

## Safety

This remains read-only.

- no gameplay methods are invoked;
- no fields or properties are written;
- no Unity object is instantiated;
- no native game object is created;
- all reads still run through `IDCMLGameThread`.

The public API is unchanged.

## Validation gate

The existing test baseline remains:

```text
208 total
208 passed
0 failed
```

A successful live proof should retain the same object counts while at least some
known scalar values become non-null, for example:

- `Server.isOn`
- `Server.currentProcessingSpeed`
- `NetworkSwitch.PortCount`
- `Router.asn`
- `SFPModule.speed`
- `CableLink.connectionSpeed`
