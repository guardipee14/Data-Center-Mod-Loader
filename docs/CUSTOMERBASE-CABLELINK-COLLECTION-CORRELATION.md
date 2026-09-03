# CustomerBase CableLink Collection Correlation Probe

The property-state proof established:

- nine live `Il2Cpp.CustomerBase` components;
- `customerBaseID` values identifying the nine base instances;
- `customerID = -1` on the observed bases;
- `customerItem = null` on all nine observed bases;
- `cableLinks` is the only property that could not previously be normalized;
- the nine CustomerBase subtrees independently contain 36 `Il2Cpp.CableLink`
  components.

This patch tests whether `CustomerBase.cableLinks` is the authoritative
relationship between each CustomerBase and those CableLink components.

## Host-neutral reference collections

`DCMLGameValueKind` gains the additive value:

```text
ReferenceCollection = 9
```

`DCMLGameValue` gains:

```text
ReferenceValues
CollectionCount
```

The MelonLoader reader only performs this collection normalization for
IL2CPP reference arrays:

```text
Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<T>
```

Enumeration is capped at 256 references. Arbitrary dictionaries, lists, and
other enumerable application objects are not automatically traversed.

Each element is normalized through the same Unity-object reference reader
already used for single references, preserving the referenced object's Unity
instance ID, name, and native type.

## Live CustomerBase correlation

For the nine exact CustomerBase components the TestModule records:

```text
LastCustomerBaseCableLinkCollectionBaseCount
LastCustomerBaseCableLinkCollectionDeclaredCount
LastCustomerBaseCableLinkCollectionReferenceCount
LastCustomerBaseCableLinkCollectionUniqueReferenceCount
LastCustomerBaseCableLinkCollectionTopologyTargetCount
LastCustomerBaseCableLinkCollectionTopologyTargetMatchCount
LastCustomerBaseCableLinkCollectionNonTargetReferenceCount
LastCustomerBaseCableLinkCollectionSample
```

Topology comparison uses `CableLink` **component instance IDs**, matching the
identity mode already proven by the topology graph.

## Interpretation

A full match would prove structural membership:

```text
CustomerBase
  -> cableLinks[]
    -> exact live CableLink components
```

and would show how many of those CableLinks are the 18 SFP-linked topology
targets.

That is still not automatically a physical device-to-device network edge.
The relationship will be classified according to the observed game contract,
not the GameObject name.

## Safety

Read-only.

No CustomerBase setup/load/update methods are invoked, no fields or
properties are written, and collection traversal is limited to IL2CPP
reference arrays with a hard cap.
