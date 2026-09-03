# Multi-Module Integration Probe

DCML v0.0.4 development includes two real module packages that validate
dependency-safe startup and cross-module event delivery through the normal
loader/runtime contracts.

## Packages

```text
dcml.probe.publisher
dcml.probe.consumer
```

The consumer declares a required dependency on publisher `>= 1.0.0`.

Neither probe module references `DCML.DataCenter`, MelonLoader, or a special
probe framework. Both are ordinary `IDCMLModule` implementations that use only
the host-neutral Core contracts.

## Handshake

The host creates one shared `IDCMLEventBus` for all module contexts.

Startup proceeds as follows:

```text
publisher.Initialize
publisher.Start
consumer.Initialize
consumer.Start
    -> publish "dcml.probe.request"
        -> publisher receives request
        -> publisher verifies it already reached Start
        -> publish "dcml.probe.response"
            -> consumer receives response
    -> consumer verifies response was received synchronously
```

A request arriving before `publisher.Start` is treated as a probe failure.

## Shutdown proof

The runtime stops running modules in reverse startup order.

During `consumer.Stop`, the consumer publishes:

```text
dcml.probe.consumer-stopping
```

The publisher must still be running and subscribed. It records
`ConsumerStopObserved` before its own `Stop`.

This verifies that dependent shutdown occurs before dependency shutdown.

## Automated integration path

The automated integration test uses:

- real manifest JSON files;
- real compiled probe DLLs;
- `DCMLPackageDiscovery`;
- `DCMLDependencyResolver`;
- `DCMLModuleRuntime`;
- the production `DCMLReflectionModuleActivator`;
- the real `DCMLEventBus`;
- package-specific module data directories.

The only test-specific component is a small module-context factory that
provides the shared event bus and temporary data directories. No module or
event behavior is mocked.

## Production activator extraction

The reflection-based dynamic activation logic previously lived directly in
`MelonModuleActivator`, even though that code had no Melon-specific behavior.

v0.0.4 extracts the implementation into:

```text
DCML.Core.Runtime.DCMLReflectionModuleActivator
```

`MelonModuleActivator` remains present and delegates to the host-neutral
implementation. This preserves the existing MelonLoader adapter surface while
allowing integration tests and future host adapters to exercise the exact same
activation behavior.

## Live Data Center validation

The patch installer can stage the same two packages into:

```text
UserData\DCML\Modules
```

On the next Data Center launch, each module writes an auditable trace to its
own module data directory:

```text
UserData\DCML\Data\dcml.probe.publisher\multimodule-probe.log
UserData\DCML\Data\dcml.probe.consumer\multimodule-probe.log
```

Expected startup evidence:

Publisher:

```text
Initialize
Start
RequestReceived
ResponsePublishing
ResponsePublished
```

Consumer:

```text
Initialize
Start
RequestPublishing
ResponseReceived
HandshakeComplete
```

The probes do not perform scene discovery, hardware scans, save access, or
game-state mutation.
