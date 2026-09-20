# 16 — Portal Item Detail Pages

**What to build:** A Detail Page for any individual Portal Item, reachable only from the Complete View (ticket 15), showing that item's own successful/failed request evolution, with the same in-page All/Last-7-Days toggle and sparse-data handling as the ArcGIS Server service Detail Pages.

**Blocked by:** 15.

**Status:** done

- [x] Detail Page generated for every Portal Item in the aggregate database, linked only from the Complete View.
- [x] Line charts span only the item's real date range — no zero-padding before its first appearance or after its last (verified with a fixture-injected sparse-data item).
- [x] In-page All/Last-7-Days toggle switches without navigating to a different page.
- [x] Shows the same live-Portal-page link as its Complete View row.

## Comments

Implemented mirroring `ArcGisServiceDetailPageBuilder`'s shape (ticket 13). Unlike a service's Detail Page, Portal's aggregate table carries only one combined `time_taken_second` figure (no successful/failed split), so this page has 2 charts (successful/failed hit-count evolution) rather than 4 — no average-time chart.

New `src/IisLogParserArcGIS.Data/Queries/ByPortalItemDetailDailyHitTotalsQuery.cs` mirrors `ByArcGisServiceDetailDailyHitTotalsQuery` but filters on `portal_item_id` alone (no site/folder/type tuple), reusing `ByPortalItemDailyHitTotalRow`'s existing shape. No SQL view: the (date, item) pair is already the base table's own grain.

New `src/IisLogParserArcGIS.Reports/Sections/PortalItemDetailPageBuilder.cs` drives its page set from `ByPortalItemRangeTotalsQuery` — the same rows the Portal Complete View (ticket 15) lists. Each page embeds both All and Last 7 Days ranges at once using the identical in-page-toggle/`<template>`-clone-on-first-click pattern ticket 13 established (avoiding drawing a Google Chart into a hidden, zero-size container). `DashboardSidebar.PortalItemDetailHref` computes each page's URL (`portal/detail/{portalItemId}.html`) but is deliberately never added to `BuildTree`, so no sidebar entry ever points to one. Wired into `RegenerationRun.Run` right after `PortalCompleteViewBuilder.Build`.

`PortalCompleteViewBuilder` gained a new "Detail Page" table column linking each row to its Detail Page — the Portal Item id column itself keeps the existing live-Portal link from ticket 15 unchanged (per this ticket's AC4, the Detail Page shows that *same* live link, so the Complete View row's own live link had to stay put rather than being repurposed). The live-Portal-link URL builder (`https://{PortalBaseUrl}/home/item.html?id=<percent-encoded id>`) was factored out of `PortalCompleteViewBuilder` into a new shared `PortalItemLiveLink.BuildHref` helper so both pages build the identical link from one place, per a `/code-review` finding (see below). `PortalItemDetailHref` embeds the raw, unescaped `portal_item_id` as a file-path segment — deliberately consistent with `ArcGisServiceDetailHref`'s own precedent (ticket 13) of embedding raw, unvalidated identifiers directly into file paths; the file written to disk and the href pointing to it use the exact same unescaped string, so they stay self-consistent even though the id itself is unvalidated (per `PortalItemIdentityParser`, confirmed by the real harvested fixture containing an id with a literal `+`).

Test coverage: `ByPortalItemDetailDailyHitTotalsQueryTests` (date-range filtering, cross-item exclusion, empty/null cases) in `Data.Tests`. `PortalItemDetailPageTests` in `Reports.Tests` drives `RegenerationRun.Run` end-to-end against the ticket-09 harvested fixture: asserts a page exists for every Portal item the Complete View lists, no sidebar entry ever links to one, the Complete View's new column links to it, the Detail Page shows the same live-Portal link as its Complete View row, exactly one file exists per item (proving the toggle stays in-page), and — using `WorkingDatabaseCopy.InsertPortalItemRows` to inject an item with hits on only two widely-separated dates — that its chart shows exactly those two dates with no zero-padded dates in between. Full suite green: 164 Domain.Tests, 100 Data.Tests, 48 Cli Tests, 4 RegressionTests, 63 Reports.Tests (379 total).

Reviewed via `/code-review`: fixed the live-link-URL duplication between `PortalCompleteViewBuilder` and `PortalItemDetailPageBuilder` (factored into `PortalItemLiveLink`) and a missing CSS rule for the new `.detail-page-live-link` class. Deliberately not changed: `PortalItemDetailHref`'s raw (non-percent-encoded) embedding of `portal_item_id` as a path segment, matching `ArcGisServiceDetailHref`'s already-accepted precedent, and because escaping only the href while writing the actual file under the raw name would break the link instead of fixing anything.
