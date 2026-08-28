# Game Type Catalog

`IDCMLGameTypeCatalog` is a read-only runtime introspection capability.

It catalogs loaded managed/IL2CPP wrapper types rather than instantiated
GameObjects. This matters because a Data Center gameplay concept may exist as a
type even when no object carrying that type is currently instantiated in the
active scene.

## API

```csharp
IReadOnlyList<DCMLGameTypeInfo> types =
    catalog.Find(
        new DCMLGameTypeQuery(
            fullNameStartsWith: "Il2Cpp.",
            nameContains: "Server",
            maxResults: 512));
```

Each result contains:

- full name
- namespace
- simple name
- assembly name
- immediate base type
- class/interface/enum/value-type flags
- abstract flag
- implemented interface names

No live game object is exposed and no game state is modified.

## Capability

Hosts that provide the catalog advertise:

```text
dcml.game.type-catalog
```

A mod does not have to use this capability to be loadable by DCML.

## Diagnostic probe

The TestModule records all loaded `Il2Cpp.*` types up to the bounded catalog
maximum and creates keyword sections for:

- Server
- Rack
- Switch
- Router
- Firewall
- Device
- Port
- SFP
- QSFP
- Cable
- Factory
- Machine
- Hacking
- Coding
- Packet

The output is written to:

```text
DCML.GameTypeCatalog.<scene>.log
```

The probe also records whether it reached the 16,384-result bound. If it did
not, the loaded `Il2Cpp.*` type universe was captured completely for that run.
