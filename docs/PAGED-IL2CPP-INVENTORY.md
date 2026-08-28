# Paged IL2CPP Component Inventory

The focused `Il2Cpp.*` inventory still reached the 16,384-object bound because
`Il2Cpp.CableLink` is extremely numerous.

This patch adds generic bounded pagination instead of raising the per-query
ceiling again.

`DCMLGameObjectQuery` now supports `skipResults`. The host keeps deterministic
ordering by scene, hierarchy path, and instance ID, then applies Skip + Take.

The optional `DataCenterComponentCatalogQuery` can enable `scanAllPages` with a
hard `maxPages` budget. The catalog aggregates counts and examples across pages.

`DataCenterComponentCatalogSnapshot` reports `PagesScanned` and `IsComplete`.
`IsComplete` becomes true only after a partial final page is observed. If every
allowed page is full, the snapshot explicitly remains incomplete.

The TestModule enables paging for its focused `Il2Cpp.*` diagnostic. Each
low-level query remains bounded, and `DCML.DataCenter` remains optional.
