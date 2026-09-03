# Paged Topology Target Resolution

The first live topology graph correctly produced 18 `sfp-link` edges, but all
18 were unresolved.

That did not indicate a bad relationship.

The ordinary hardware snapshot intentionally captures only 64 CableLink
components per type. Data Center had previously shown more than twenty
thousand scene CableLink components, and the 18 SFP target instance IDs were
outside that bounded 64-item slice.

## Correction

`DataCenterHardwareTopology.CaptureAsync` now:

1. takes the normal bounded hardware snapshot;
2. collects the exact CableLink instance IDs referenced by live SFP modules;
3. keeps any target already present in the bounded snapshot;
4. if targets remain, queries only `Il2Cpp.CableLink` identity state;
5. requests no CableLink member values;
6. pages with the low-level reader maximum of 16,384 results;
7. stops as soon as every target ID is found;
8. otherwise stops when CableLink results are exhausted.

Normal `DataCenterHardwareSnapshots` behavior is unchanged.

## Identity

Resolution remains based on captured Unity `InstanceId`.

Names are never used as keys.

This matters because live references looked like:

```text
Il2Cpp.CableLink#426278:SFP_Slot2.003
```

## Graph scope

The live graph now contains:

- live SFP nodes;
- only CableLink nodes that are direct targets of those SFP links;
- `sfp-link` edges.

It no longer includes 64 arbitrary CableLink nodes merely because they happened
to occupy the first snapshot page.

## Diagnostics

The graph now reports:

```text
CableSearchPages
CableCandidatesScanned
CableSearchExhausted
```

`CableSearchExhausted` is true only when the CableLink identity search reached
the end while unresolved targets still remained.

## Safety

The paging query reads identity only:

- no CableLink gameplay properties;
- no gameplay method invocation;
- no mutation;
- no native pointer exposure.

All low-level reads still use `IDCMLGameComponentStateReader`, which is
marshalled through `IDCMLGameThread` by the MelonLoader host.
