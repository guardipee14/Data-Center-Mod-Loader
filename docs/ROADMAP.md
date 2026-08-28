# DCML Roadmap

## Proven runtime foundation

- [x] Manifest model and schema version
- [x] JSON serialization/deserialization
- [x] Manifest validation
- [x] Semantic-version validation and comparison
- [x] Package discovery
- [x] Duplicate module detection
- [x] Required dependencies
- [x] Optional dependencies
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
- [x] 80-test automated baseline

## Next

- [ ] First Data Center-facing API abstraction
- [ ] Game lifecycle/event abstraction
- [ ] Game object/entity discovery abstraction
- [ ] Versioned API capability surface
- [ ] Multi-module integration probe
- [ ] CI build/test workflow
- [ ] Reproducible release packaging
- [ ] API reference documentation

## Later

- [ ] Additional host adapters
- [ ] Workshop package source/staging adapter
- [ ] Update/version policy
- [ ] Module diagnostics/status UI
- [ ] DC Architect module
- [ ] Investigate sanctioned cloud/Boosteroid bootstrap options

## Explicit non-claim

DCML currently requires an existing compatible host such as MelonLoader. The project does not currently provide a standalone Boosteroid bootstrap.
