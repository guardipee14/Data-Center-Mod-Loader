# DCML Architecture

## Design goal

DCML separates module code from the mechanism that injects or hosts it.

The current implementation uses MelonLoader because it provides a working CoreCLR environment inside Data Center. DCML Core itself does not require MelonLoader.

```text
                   Host-neutral
+-----------------------------------------------+
|                  DCML.Core                    |
|                                               |
|  manifests -> discovery -> dependencies       |
|                     -> runtime -> services    |
+-----------------------------------------------+
                     ^
                     |
              host adapter boundary
                     |
+-----------------------------------------------+
|        DCML.Loader.MelonLoader                |
|  Melon lifecycle + assembly activation        |
|  host-backed logging/runtime information      |
+-----------------------------------------------+
                     ^
                     |
                 MelonLoader
                     ^
                     |
                 Data Center
```

## Package pipeline

The implemented package pipeline is:

```text
manifest
   |
discovery
   |
validation
   |
dependency resolution
   |
deterministic load order
   |
activation
   |
Initialize
   |
Start
   |
Running
   |
Stop
```

A bad package does not prevent unrelated valid packages from being discovered.

Required dependency failures propagate to dependents. Optional dependencies do not block startup.

## Runtime lifecycle

`DCMLModuleRuntime` coordinates the module lifecycle without knowing how assemblies are loaded.

Host-specific activation is supplied through `IDCMLModuleActivator`.

Host-specific context construction is supplied through `IDCMLModuleContextFactory`.

This keeps the runtime coordinator testable outside the game.

## Services

Every module receives an `IDCMLModuleContext`.

```csharp
public interface IDCMLModuleContext
{
    string ModuleDirectory { get; }
    string DataDirectory { get; }
    IServiceProvider Services { get; }
}
```

The current MelonLoader host registers:

- `IDCMLLogger`
- `IDCMLRuntimeInfo`
- `IDCMLConfiguration`
- `IDCMLEventBus`

### Logging

`IDCMLLogger` is module-scoped. The MelonLoader host currently forwards it into MelonLoader logging with the manifest module ID.

### Runtime information

`IDCMLRuntimeInfo` exposes:

- module ID;
- DCML version;
- host name/version;
- game name/root;
- advertised capabilities.

Capabilities are string identifiers so modules can feature-detect APIs.

### Configuration

Each module gets one persistent configuration file:

```text
UserData\DCML\Data\<module-id>\config.json
```

`IDCMLConfiguration` provides typed load/save operations.

### Events

The host creates one `IDCMLEventBus` instance for the DCML runtime and shares it across module contexts.

Modules can subscribe to and publish typed events without directly referencing one another.

## Host boundary

The MelonLoader adapter is intentionally replaceable.

A future host needs to provide, at minimum:

1. module assembly/type activation;
2. module context creation;
3. lifecycle entry/exit integration;
4. host-specific service implementations.

DCML modules should not need to change simply because the host changes.

## Current cloud limitation

Steam Workshop can stage files for Data Center, but the shipping IL2CPP loader was not found to provide a working arbitrary managed-code execution path.

The current repository therefore does **not** claim to bootstrap DCML on Boosteroid by itself.

A cloud first-stage still requires a sanctioned execution path such as provider support, developer support, or an already-present compatible host.
