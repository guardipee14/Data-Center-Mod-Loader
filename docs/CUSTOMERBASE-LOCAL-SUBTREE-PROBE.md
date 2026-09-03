# CustomerBase Local Subtree Probe

The hierarchy-context live proof established that the 18 SFP-slot CableLinks
are contained under objects shaped like:

```text
SFP_Slot*.003 [Il2Cpp.CableLink]
  -> Switch4xSFP [outline/listener only]
    -> CustomerBase [Il2Cpp.CustomerBase]
```

No `Il2Cpp.NetworkSwitch`, Router, Firewall, Server, PatchPanel, Internet, or
Rack component appeared on the ancestor chain.

That means the device gameplay component may be attached to a sibling or
descendant inside the same local CustomerBase subtree.

## Generic discovery addition

`DCMLGameObjectQuery` now accepts optional:

```text
ParentInstanceIds
```

This means "return GameObjects whose direct parent GameObject has one of these
exact Unity instance IDs."

The MelonLoader host applies the parent-ID check before `TryCreateInfo`, so
nonmatching scene objects do not pay the component-enumeration cost.

All existing object-discovery filters continue to combine normally.

## Live probe

The test module identifies the exact `Il2Cpp.CustomerBase` ancestor from each
SFP-slot chain, deduplicates those roots, and walks direct children recursively
for at most six levels.

It reports exact object counts for:

```text
Il2Cpp.NetworkSwitch
Il2Cpp.Router
Il2Cpp.Firewall
Il2Cpp.Server
Il2Cpp.PatchPanel
Il2Cpp.Internet
Il2Cpp.Rack
Il2Cpp.CableLink
```

It also reports every distinct `Il2Cpp.*` type found in the local subtrees and
prints up to 24 objects that carry native components.

This deliberately allows unfamiliar game component types to surface instead
of assuming the switch behavior must be on `Il2Cpp.NetworkSwitch`.

## Interpretation

CustomerBase subtree membership is scene-structure evidence only. It is not
automatically promoted to a network edge or device ownership contract.

## Safety

The probe is read-only:

- no game methods are invoked;
- no values are written;
- no native pointers are exposed;
- traversal is local to exact CustomerBase roots;
- recursion is capped at six descendant levels.
