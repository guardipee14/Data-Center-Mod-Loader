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

## Explicit save selection

The adapter never searches a save directory.

The caller must supply:

- the process host path;
- the persistence-helper DLL path;
- the exact save-file path.

Example:

```csharp
IDataCenterCablePersistenceSource persistence =
    new DataCenterProcessCablePersistenceSource(
        hostPath,
        helperDllPath,
        explicitlySelectedSavePath);

DataCenterApi api =
    DataCenterApi.Create(
        context,
        persistence);
```

The supplied save path is normalized with `Path.GetFullPath` and passed
directly to the helper process.

DCML does not:

- enumerate save files;
- sort saves by modification time;
- select the newest save;
- guess which save belongs to the player.

This keeps persistence selection explicit and prevents a mod from silently
reading an unintended save.

## Read-only behavior

The adapter reads persistence information only.

It launches the helper with the explicitly selected save path, reads JSON from
standard output, and converts that data into
`DataCenterCablePersistenceSnapshot`.

It does not write to the save file.

## TestModule migration

`DCML.TestModule` now consumes the reusable
`DataCenterProcessCablePersistenceSource` instead of owning a private copy of
the process/JSON adapter.

The diagnostic probe remains opt-in. Moving the source does not enable
persistence probing by default and does not change scene-initialization
behavior.
