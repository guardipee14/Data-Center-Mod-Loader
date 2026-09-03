# Game Resource Discovery

`IDCMLGameResourceDiscovery` is a read-only host capability for inspecting
loaded **non-scene GameObjects**.

It complements `IDCMLGameObjectDiscovery`:

- `IDCMLGameObjectDiscovery` returns objects that belong to valid, loaded Unity
  scenes.
- `IDCMLGameResourceDiscovery` intentionally excludes those objects and returns
  loaded GameObjects whose scene is invalid or not loaded.

## Why "resource" and not "prefab"

Unity's `Resources.FindObjectsOfTypeAll<GameObject>()` can expose prefab assets
and other loaded non-scene GameObjects.

DCML therefore does **not** claim that every result is a prefab. The public API
uses the more accurate term "resource".

No object is instantiated, modified, enabled, disabled, or saved by this API.

## Capability

Hosts that provide this service advertise:

```text
dcml.game.resource-discovery
```

A mod does not need this capability, `DCML.DataCenter`, or any other optional
high-level API in order to be loadable by DCML.

## API

```csharp
IDCMLGameResourceDiscovery? resources =
    context.Services.GetService(
        typeof(IDCMLGameResourceDiscovery))
    as IDCMLGameResourceDiscovery;

IReadOnlyList<DCMLGameResourceInfo> servers =
    resources?.Find(
        new DCMLGameResourceQuery(
            componentTypeName: "Il2Cpp.Server",
            maxResults: 64))
    ?? Array.Empty<DCMLGameResourceInfo>();
```

Each immutable result exposes:

- `InstanceId`
- `Name`
- `ComponentTypeNames`

Component identities use the same native IL2CPP identity resolution used by
scene object discovery, so a native `Server` component is represented as
`Il2Cpp.Server` rather than a collapsed managed wrapper type.

## Query filters

`DCMLGameResourceQuery` supports:

- `NameContains`
- `ComponentTypeName` (full or simple exact name)
- `ComponentTypeNamePrefix`
- `MaxResults`
- `SkipResults`

Results are deterministic by name and instance ID.

## Current live probe

The TestModule performs targeted read-only resource queries for:

- `Il2Cpp.Server`
- `Il2Cpp.Rack`
- `Il2Cpp.NetworkSwitch`
- `Il2Cpp.Router`
- `Il2Cpp.Firewall`
- `Il2Cpp.CableLink`

The lifecycle proof records:

```text
GameResourceDiscoveryRuns
LastGameResourceDiscoveryScene
LastGameResourceDiscoveryServerCount
LastGameResourceDiscoveryRackCount
LastGameResourceDiscoveryNetworkDeviceCount
LastGameResourceDiscoveryCableCount
LastGameResourceDiscoveryError
LastGameResourceDiscoverySample
```

This is intended to answer whether the physical gameplay types proven by the
runtime type catalog also exist as loaded non-scene GameObjects before the
player places them into a scene.

## Limits

A non-scene GameObject result proves that the object is loaded and is not a
normal member of a currently loaded scene. It does not, by itself, prove where
the object originated, that it is a shop item, or that it is safe to
instantiate.

Those behaviors require separate evidence before DCML exposes any write/control
API.
