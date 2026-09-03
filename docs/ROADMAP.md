# DCML Roadmap

DCML is a host-neutral mod loader/runtime for **Data Center**. Its loader job is
to discover, validate, order, activate, and lifecycle-manage compatible mods
while bridging them into the current game/runtime host.

DCML does **not** require mods to use the optional DCML development APIs.
Developers may use DCML helpers, lower-level Unity/IL2CPP/game/host APIs, or a
mixture of those approaches when the current host can load the mod.

## Published milestones

### v0.0.1 — Runtime Foundation

- [x] Manifest model and schema version
- [x] JSON serialization/deserialization
- [x] Manifest validation
- [x] Semantic-version validation and comparison
- [x] Package discovery
- [x] Duplicate module detection
- [x] Required and optional dependencies
- [x] Minimum dependency versions
- [x] Dependency-cycle detection
- [x] Deterministic dependency-safe load order
- [x] Host-neutral runtime lifecycle
- [x] Failure isolation
- [x] Reverse-order shutdown
- [x] MelonLoader host adapter
- [x] Real in-game dynamic module activation
- [x] Logging service
- [x] Runtime-information service
- [x] Persistent configuration service
- [x] Shared typed event bus
- [x] Live lifecycle proof: Initialize -> Start -> Event -> Stop
- [x] First GitHub prerelease packaging

### v0.0.2 — Data Center Discovery Foundation

- [x] Data Center-facing optional helper API
- [x] Game scene lifecycle abstraction
- [x] Read-only Unity GameObject discovery
- [x] Paged object discovery
- [x] Loaded runtime/IL2CPP type catalog
- [x] Inheritance-aware semantic discovery
- [x] Evidence-backed entity classification
- [x] Game resource discovery
- [x] Game main-thread scheduler
- [x] Runtime type inspection
- [x] CI build/test workflow
- [x] Reproducible GitHub prerelease packaging
- [x] Live Data Center validation

### v0.0.3 — Evidence-Backed Physical Topology

- [x] Read-only component-state snapshots
- [x] Evidence-backed hardware snapshots
- [x] Live hardware topology graph
- [x] Persistent hardware identity without fabricating Unity instance IDs
- [x] Structural relationship modeling
- [x] Preserve SFP-module insertion as structural rather than physical cabling
- [x] Evidence-backed physical cable persistence model
- [x] One bidirectional `NetworkConnection` edge per persisted cable segment
- [x] Persistent cable IDs and endpoint-resolution evidence
- [x] Server, switch, router, firewall, patch-panel, patch-panel-port, and customer-base endpoint resolution
- [x] Explicit read-only save selection
- [x] Host-neutral cable persistence-source contract
- [x] Out-of-process .NET 8 NRBF persistence helper
- [x] Keep NRBF / incompatible metadata dependencies outside the MelonLoader .NET 6 process
- [x] Scene-initialization safety guardrails
- [x] Heavy automatic diagnostics disabled by default
- [x] One-shot runtime persistence validation probe
- [x] Shared `DCML.DataCenter.dll` deployment synchronization
- [x] Runtime version aligned to 0.0.3
- [x] 306-test automated baseline
- [x] GitHub CI green on the exact release commit
- [x] Live proof: 686 cables, 1,372 / 1,372 resolved endpoints, 686 bidirectional physical edges
- [x] Published `v0.0.3` prerelease with ZIP and SHA-256

## Next milestone — v0.0.4 API Surface & Multi-Module Interoperability

The next milestone should turn the proven internal/runtime capabilities into a
cleaner developer-facing platform without making those APIs mandatory for mod
compatibility.

### Loader/runtime surface

- [ ] Define a versioned capability contract so mods can test both capability presence and API version
- [ ] Document capability compatibility rules and fallback behavior
- [ ] Add a multi-module integration probe with dependency-safe startup and cross-module event delivery
- [ ] Add integration coverage for optional dependency present / absent behavior across real module packages
- [ ] Add package-level compatibility diagnostics for unsupported DCML/API requirements
- [ ] Keep loader acceptance independent from optional SDK/Data Center helper usage

### Optional SDK boundary

- [ ] Formalize the separation between minimal loader/runtime contracts and optional developer conveniences
- [ ] Identify which existing convenience services belong in a future `DCML.SDK` surface
- [ ] Preserve backwards-compatible access while the split is introduced
- [ ] Add concise examples for logging, configuration, events, scene lifecycle, main-thread work, and discovery
- [ ] Publish API reference documentation for the stable/provisional capability surface

### Data Center integration

- [ ] Move the proven process-backed cable persistence source out of the TestModule into a reusable Data Center/host adapter
- [ ] Keep explicit save selection; do not silently choose the newest save
- [ ] Add production-facing persistence-source configuration without embedding user-specific paths in release packages
- [ ] Add reusable topology capture examples for mod authors
- [ ] Expand physical-path reasoning only from evidence-backed persisted/live relationships
- [ ] Preserve read-only defaults and scene-initialization safety

### Validation and release engineering

- [ ] Add release packaging to CI or a reproducible repository-owned release script
- [ ] Validate release artifacts against their source commit and SHA-256 automatically
- [ ] Keep shared-assembly hash checks in the release gate
- [ ] Keep persistence helper dependency-isolation checks in the release gate
- [ ] Add a release checklist that requires live proof only when runtime-facing behavior changes

## Follow-on milestones

### v0.0.5 — Diagnostics & Module Status

- [ ] Module diagnostics/status model
- [ ] Surface discovery/validation/dependency/activation failures in a developer-friendly form
- [ ] Runtime module inventory and lifecycle state reporting
- [ ] Optional in-game status UI
- [ ] Exportable diagnostic report that avoids sensitive/user-specific data

### v0.0.6 — Package Sources & Workshop Staging

- [ ] Package-source abstraction
- [ ] Workshop package source/staging adapter
- [ ] Safe package/update metadata model
- [ ] Update/version policy
- [ ] Dependency-aware update planning
- [ ] Never bypass platform/provider restrictions

### v0.1.0 — Public API Stabilization

- [ ] Review and stabilize public loader/runtime contracts
- [ ] Stabilize versioned capability policy
- [ ] Stabilize optional SDK/Data Center helper boundaries
- [ ] Publish migration guidance for provisional APIs
- [ ] Expand multi-module compatibility tests
- [ ] Define supported host/version matrix

## Later

- [ ] Additional host adapters
- [ ] DC Architect module
- [ ] Investigate sanctioned cloud/Boosteroid bootstrap options
- [ ] Explore a cloud-safe persistence decoder only if the execution environment permits it
- [ ] Additional Data Center domain helpers as runtime evidence supports them
- [ ] Factory, hacking, coding, and other game-system APIs only when evidence and use cases justify them

## Safety and compatibility principles

- Prefer read-only discovery and inspection until mutation is explicitly designed and proven safe.
- Do not invoke game load/setup/update methods merely to inspect state.
- Avoid unknown property getters during sensitive scene initialization because getters may have side effects.
- Heavy diagnostics must not synchronously run during scene initialization.
- Do not infer physical connectivity from names alone.
- `SFPModule.link` remains structural `sfp-module-insertion`, not a physical network connection.
- Physical `NetworkConnection` edges require persisted/observed endpoint evidence.
- Optional DCML APIs are conveniences, not loader acceptance requirements.
- Do not upload proprietary Data Center, MelonLoader, or other third-party binaries to the repository.
- Do not bypass Steam, Boosteroid, cloud-provider, or platform execution restrictions.

## Explicit non-claim

DCML currently requires an existing compatible host such as MelonLoader.

The project does **not** currently provide a standalone Boosteroid/cloud-gaming
bootstrap. The out-of-process persistence helper is proven for the local
MelonLoader environment only and should not be presented as a cloud-safe
bootstrap or cloud compatibility proof.
