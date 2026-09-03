# CustomerBase Property State Probe

The live `Il2Cpp.CustomerBase` type inspection established that its 316
reported fields are dominated by IL2CPP interop wrapper metadata such as:

```text
NativeFieldInfoPtr_*
NativeMethodInfoPtr_*
```

The game-facing CustomerBase state is exposed through public properties.

## Targeted live properties

This probe reads only these direct, non-static, readable properties from the
nine exact live CustomerBase components:

```text
cableLinks
currentSpeed
currentTotalAppSpeeRequirements
customerBaseID
customerID
customerItem
howLongToWaitBeforeFine
maximumAppRequirementsSpeedTotal
wantsInternet
wasFullySatisfied
```

The property list is an explicit allowlist.

No setup, load, update, coroutine, or gameplay methods are invoked.

## Why customerItem matters

The type inspection showed:

```text
CustomerBase.customerItem : Il2Cpp.CustomerItem
CustomerBase.LoadData(CustomerBaseSaveData)
CustomerBase.SetUpApp(..., CustomerBaseSaveData)
CustomerBase.SetUpBase(CustomerItem, CustomerBaseSaveData)
```

Therefore `CustomerItem` and `CustomerBaseSaveData` are directly implicated
as model/setup/persistence structures for CustomerBase.

This patch also adds both types to the normal detailed inspection log.

A compact lifecycle proof line reports their direct readable properties and
direct instance fields:

```text
LastCustomerBaseRelatedTypeSummary
```

This is metadata-only. It does not instantiate, mutate, load, or save either
type.

## Proof compatibility

The older field diagnostics remain present and remain zero for CustomerBase
runtime state:

```text
LastCustomerBaseStateProbeFieldCount
LastCustomerBaseStateProbeFields
```

New property-specific diagnostics are:

```text
LastCustomerBaseStateProbePropertyCount
LastCustomerBaseStateProbeProperties
```

The existing value/reference/scalar/null/unsupported/unavailable counters and
sample output now describe the selected property reads.

## Safety

Read-only:

- exact nine CustomerBase GameObjects;
- explicit property allowlist;
- no gameplay methods;
- no writes;
- no native pointer exposure;
- no collection enumeration in this probe.
