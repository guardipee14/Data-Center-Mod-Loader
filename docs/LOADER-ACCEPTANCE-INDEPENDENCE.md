# Loader Acceptance Independence

DCML's loader acceptance rules are independent from the optional DCML SDK,
Data Center helper APIs, and host convenience services.

A package needs only:

- a valid manifest;
- a compatible DCML version when `minimumDCMLVersion` is declared;
- any runtime capabilities that the package **explicitly** lists in
  `requiredCapabilities`;
- satisfied required module dependencies;
- an entry assembly/type that can be activated by the current host.

DCML does not infer optional API requirements from the fact that DCML is the
loader.

## Minimal real-package proof

v0.0.4 includes:

```text
dcml.probe.loader-acceptance-minimal
```

The project references only:

```text
DCML.Core
```

It does not reference:

```text
DCML.DataCenter
DCML.Loader.MelonLoader
MelonLoader
```

Its module uses only the minimal loader contract:

```text
IDCMLModule
IDCMLModuleContext.ModuleDirectory/DataDirectory
```

It does not request services from `IDCMLModuleContext.Services`.

Its manifest intentionally omits `requiredCapabilities`.

## Automated acceptance proof

The real package is staged into a clean temporary modules root and exercised
through:

```text
DCMLPackageDiscovery
        |
DCMLPackageCompatibilityEvaluator
        |
DCMLDependencyResolver
        |
DCMLReflectionModuleActivator
        |
DCMLModuleRuntime
```

For the compatibility phase, the simulated host advertises an **empty**
capability catalog.

For activation, the module receives an `IServiceProvider` that returns `null`
for every service lookup.

The package must still:

```text
discover
validate
remain compatible
resolve
activate
Initialize
Start
reach Running
Stop cleanly
```

This proves that logging, configuration, events, scene lifecycle, discovery,
Data Center helpers, and other optional APIs are not implicit loader
requirements.

## Capability requirements remain opt-in

A package that needs a DCML capability may declare it:

```json
{
  "requiredCapabilities": [
    {
      "id": "dcml.events",
      "minimumVersion": "1.0.0"
    }
  ]
}
```

Only those explicit requirements participate in capability compatibility
blocking.

A package that does not use those APIs may omit `requiredCapabilities`
entirely.

## Advanced / lower-level modules

This acceptance rule does not prohibit modules from using compatible
lower-level host, Unity, IL2CPP, or game APIs directly.

Those dependencies are the module author's responsibility and must be
loadable in the active host environment. DCML does not require the optional
SDK or `DCML.DataCenter` layer merely because a module chooses a lower-level
integration path.

## Live proof

The same minimal package is staged into Data Center under:

```text
UserData\DCML\Modules\DCML.LoaderAcceptanceProbe.Minimal
```

The package directory contains only:

```text
DCML.LoaderAcceptanceProbe.Minimal.dll
manifest.json
```

On successful live activation it writes:

```text
UserData\DCML\Data\dcml.probe.loader-acceptance-minimal\loader-acceptance-probe.log
```

with:

```text
Initialize
Start
Stop
```

There must be no compatibility or runtime failure diagnostic for the module,
and the probe must complete Initialize, Start, and Stop successfully.
