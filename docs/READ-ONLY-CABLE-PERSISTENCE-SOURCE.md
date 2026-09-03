# Read-Only Cable Persistence Source — Process Boundary

## Why a process boundary

The physical cable model is valid and the 303-test source gate passed, but
runtime smoke testing showed that System.Formats.Nrbf 9.x brings a
System.Reflection.Metadata 9.x dependency into a .NET 6 process where
System.Reflection.Metadata is already supplied by the .NET 6 shared framework.

DCML therefore does not load that dependency closure in the game process.

For the local MelonLoader proof:

```text
Data Center / MelonLoader / DCML (.NET 6)
        |
        | child process, stdout JSON only
        v
DCML.Persistence.Helper (.NET 8)
        |
System.Formats.Nrbf 10.0.11
        |
explicit read-only .save
```

The game process receives only JSON and converts it into DCML-owned persistence
DTOs.

## Safety

- the save path is explicit;
- the first proof remains SHA-256 gated by the installer;
- the helper opens the save read-only;
- no BinaryFormatter deserialization;
- no game method calls;
- no scene scan;
- no save write;
- no System.Formats.Nrbf assembly enters the MelonLoader load context;
- stale in-process bridge files are backed up and removed.

## Scope

This is a safe local host adapter. It is **not** claimed as the final
Boosteroid/cloud implementation because a cloud provider may not permit a child
process or may not provide the required .NET 8 runtime.

The persistence-source interface remains host-neutral so a future native or
cloud-safe decoder can replace the process adapter without changing physical
topology semantics.
## MelonLoader shared-assembly deployment

`DCML.DataCenter.dll` is a shared DCML library and MelonLoader loads the copy
from:

```text
UserLibs\DCML.DataCenter.dll
```

before the TestModule starts.

The TestModule package may also carry a matching copy under its module
directory, but the two files must be byte-identical. A stale `UserLibs` copy
can shadow a newer module-local copy because the assembly identity has already
been loaded into the process.

The validated installer therefore updates both locations from the same Release
build and verifies their SHA-256 hashes match before launch.

## One-shot validation probe

`EnablePhysicalCablePersistenceSourceProbe` is a validation switch, not a
normal recurring runtime feature.

After a probe finishes, whether successful or failed, the TestModule posts a
configuration update through the game-thread scheduler and resets the flag to:

```json
false
```

The persistence source itself remains enabled; only the proof run is
one-shot.

## Repository helper project

The out-of-process decoder used by the validated local MelonLoader adapter is
source-controlled at:

```text
src/DCML.Persistence.Helper
```

It targets `net8.0` and owns the `System.Formats.Nrbf` dependency.

No game-side project references this helper project or its NRBF dependency.
The boundary remains:

```text
MelonLoader / DCML (.NET 6)
        |
        | process + JSON
        v
DCML.Persistence.Helper (.NET 8)
```

A repository build compiles the helper, and the repository validation tests
assert that the game-side projects remain free of helper/NRBF references.
