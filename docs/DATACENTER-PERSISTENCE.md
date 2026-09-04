# Data Center Persistence Adapter

DCML keeps persisted physical-cable decoding outside the core
`DCML.DataCenter` assembly.

## Why this is a separate adapter

`DCML.DataCenter` targets both `netstandard2.1` and `net6.0`.

The proven persistence decoder path uses an out-of-process helper and modern
process APIs. The reusable implementation therefore lives in the optional
`DCML.DataCenter.Persistence` net6 host-adapter assembly.

Dependency direction:

```text
DCML.Core
    ^
    |
DCML.DataCenter
    ^
    |
DCML.DataCenter.Persistence
```

`DCML.DataCenter.Persistence` does not reference
`DCML.Persistence.Helper`. It launches a caller-supplied helper assembly out of
process and consumes its JSON output through the decoder-agnostic
`IDataCenterCablePersistenceSource` contract.

## Production configuration

DCML already provides each module with module-owned persistent configuration
through `IDCMLConfiguration`.

The MelonLoader host stores that configuration at:

```text
UserData\DCML\Data\<module-id>\config.json
```

A production module should include
`DataCenterProcessCablePersistenceSettings` inside its own settings model
instead of shipping a pre-populated machine-specific configuration file.

Example:

```csharp
public sealed class MySettings
{
    public DataCenterProcessCablePersistenceSettings CablePersistence
        { get; set; } =
        new();
}
```

The reusable persistence settings deliberately default to:

```csharp
Enabled = false
SavePath = string.Empty
HelperHostPath = string.Empty
HelperDllPath = string.Empty
```

Therefore release packages must not contain machine-specific save paths,
process-host paths, or helper paths.

The user, a configuration UI, or module-specific setup code supplies those
values in the module's persistent `config.json` after installation.

A module can then create the optional source without owning persistence-source
construction logic:

```csharp
IDCMLConfiguration configuration =
    (IDCMLConfiguration)context.Services.GetService(
        typeof(IDCMLConfiguration))!;

MySettings settings =
    configuration.Load(
        new MySettings());

IDataCenterCablePersistenceSource? persistence =
    DataCenterProcessCablePersistenceSourceFactory.Create(
        settings.CablePersistence);

DataCenterApi api =
    DataCenterApi.Create(
        context,
        persistence);
```

The factory returns `null` when settings are disabled or incomplete. It never
discovers saves and never modifies configuration.

Because the configuration file belongs to the module, the persistence adapter
does not call `Load`, `Save`, or `Delete` itself. This prevents it from
overwriting unrelated module settings.

## Explicit save selection

The adapter never searches a save directory.

The caller must supply:

- the process host path;
- the persistence-helper DLL path;
- the exact save-file path.

The supplied save path is normalized with `Path.GetFullPath` and passed
directly to the helper process.

DCML does not:

- enumerate save files;
- sort saves by modification time;
- select the newest save;
- guess which save belongs to the player.

Blank paths are rejected by the direct source constructor. The production
factory stays disabled when its settings are incomplete.

This keeps persistence selection explicit and prevents a mod from silently
reading an unintended save.

## Read-only behavior

The adapter reads persistence information only.

It launches the helper with the explicitly selected save path, reads JSON from
standard output, and converts that data into
`DataCenterCablePersistenceSnapshot`.

It does not write to the save file.

## TestModule compatibility

`DCML.TestModule` consumes the reusable factory but keeps its existing
configuration property names:

- `EnablePhysicalCablePersistenceSource`
- `PhysicalCableSavePath`
- `PhysicalCableHelperHostPath`
- `PhysicalCableHelperDllPath`

This preserves existing local probe configuration while proving the same
production-facing settings/factory path used by other modules.

The diagnostic probe remains opt-in. This change does not enable persistence
probing by default and does not change scene-initialization behavior.
