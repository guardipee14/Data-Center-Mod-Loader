# Hardware Definition/Instance Proof Fix

The first definition/instance split patch compiled and installed successfully,
but its TestModule proof instrumentation did not assign the new counters in the
successful snapshot path.

That caused the lifecycle log to show:

```text
LastHardwareSnapshotServerDefinitionCount: 0
...
```

even while the existing sample clearly showed resource-backed servers, racks,
and network devices.

This corrective patch changes only `DCML.TestModule`:

- assigns all definition/instance counters from the snapshot set;
- resets those counters in the error path;
- labels sample entries as `definition` or `instance`;
- adds the split counts to the TestModule diagnostic logger.

No loader, host, reflection, scheduler, or DataCenter API behavior changes.

Expected test gate remains:

```text
216 total
216 passed
0 failed
```
