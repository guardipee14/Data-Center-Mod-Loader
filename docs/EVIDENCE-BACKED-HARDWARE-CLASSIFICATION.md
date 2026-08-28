# Evidence-Backed Hardware Classification

This document records the first non-UI default semantic rules in the optional
`DCML.DataCenter` helper library.

These rules are recommendations for mod authors. They are not loader
requirements and they do not change which mods DCML can load.

## Evidence source

The read-only loaded game type catalog captured the complete loaded
`Il2Cpp.*` wrapper type universe for the tested Data Center runtime without
hitting its 16,384-result bound.

The catalog established these direct runtime relationships:

- `Il2Cpp.Server` derives from `Il2Cpp.UsableObject`
- `Il2Cpp.Rack` derives from `UnityEngine.MonoBehaviour`
- `Il2Cpp.NetworkSwitch` derives from `Il2Cpp.UsableObject`
- `Il2Cpp.Router` derives from `Il2Cpp.NetworkSwitch`
- `Il2Cpp.Firewall` derives from `Il2Cpp.NetworkSwitch`
- `Il2Cpp.CableLink` derives from `Il2Cpp.Interact`

The scene component inventory independently observed large numbers of
`Il2Cpp.CableLink` instances.

## Default rules

| Component type | Semantic kind | Rule ID |
| --- | --- | --- |
| `Il2Cpp.Server` | `server` | `dcml.datacenter.server.component` |
| `Il2Cpp.Rack` | `rack` | `dcml.datacenter.rack.component` |
| `Il2Cpp.NetworkSwitch` | `network-device` | `dcml.datacenter.network-switch.component` |
| `Il2Cpp.Router` | `network-device` | `dcml.datacenter.router.component` |
| `Il2Cpp.Firewall` | `network-device` | `dcml.datacenter.firewall.component` |
| `Il2Cpp.CableLink` | `cable` | `dcml.datacenter.cable-link.component` |

These are exact component-type matches rather than fuzzy hierarchy/name
guesses.

## Deliberate non-classifications

`Il2Cpp.RackMount` remains `unknown` because evidence identifies it as a rack
mounting position, not the rack itself.

`Il2Cpp.SFPModule` remains `unknown` at the top-level entity-kind layer because
it is a network transceiver/module rather than a complete network device.

`RouterConfiguration`, `FirewallConfiguration`, and
`NetworkSwitchConfiguration` remain UI/configuration helpers rather than
physical network-device entities.

No default `machine` rules are added because the loaded type catalog found no
`Factory` or `Machine` keyword matches in the tested runtime.

No default hacking/coding rules are added because those keyword probes also
returned no direct loaded type matches. Future evidence may identify those
systems under different terminology.

## Compatibility

Developers may still:

- use `DCML.DataCenter` defaults;
- provide their own `DataCenterEntityRule` set;
- use lower-level `IDCMLGameObjectDiscovery`;
- use `IDCMLGameTypeCatalog`;
- work directly with compatible Unity/IL2CPP/host APIs.

The semantic layer remains optional and recommended only.
