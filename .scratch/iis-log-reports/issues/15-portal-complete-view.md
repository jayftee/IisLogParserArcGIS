# 15 — Portal section-wide Complete View

**What to build:** A single, uncapped, paged sortable table listing every Portal Item (hits, total time taken, average time taken, successful hits, failed hits), each row's item id rendered as a link to that item's live page on Portal.

**Blocked by:** 10 (independent of, and can run in parallel with, ticket 14).

**Status:** done

- [x] Complete View lists every Portal Item in the aggregate database, uncapped, with `google.visualization.Table`'s `page: 'enable'` paging turned on.
- [x] Each row's `portal_item_id` is a clickable link built from the new `PortalBaseUrl` setting (`https://<PortalBaseUrl>/home/item.html?id=<id>`) — no live Portal REST call made at generation time.
- [x] Sortable by column header.
- [x] Verified against the fixture database's real Portal Item ids.

## Comments

Implemented mirroring `ArcGisServerCompleteViewBuilder`'s shape (ticket 12) and `PortalSectionBuilder`'s own reasoning (ticket 14) for the no-view / no-Root-filter decisions.

New `src/IisLogParserArcGIS.Data/Queries/ByPortalItemRangeTotalsQuery.cs` sums `aggregated_by_portal_item` `GROUP BY portal_item_id` over an arbitrary date range - no SQL view needed (the table's grain already matches, same reasoning as `ByRootRangeTotalsQuery`/`ByArcGisServiceRangeTotalsQuery`), no site/Root filter (the table is already scoped to the configured Portal Web Adaptor at ingest time, per ticket 04). Returns `PortalItemRangeTotalRow`.

New `src/IisLogParserArcGIS.Reports/Sections/PortalCompleteViewBuilder.cs` builds the single Complete View page: `GoogleChartsRenderer.RenderTable` with `TablePaging.Enabled`/`TableCellContent.Html` (column-header sort needs no new code, per ticket 02's superseded decision). Each row's Portal item id is wrapped in a Google Charts `{v, f}` formatted-value cell - `v` the raw id (what the table sorts on), `f` an `<a>` link to `https://{PortalBaseUrl}/home/item.html?id=<id>` (per ticket 05's resolution: no title resolution, no live Portal REST call). The id is percent-encoded via `Uri.EscapeDataString` before being substituted into the href, since `PortalItemIdentityParser` copies the raw URI segment verbatim (up to 64 chars) with no character allow-listing - the real harvested fixture in fact contains at least one id containing a literal `+`, which surfaced this during testing and confirmed the escaping is necessary, not just defensive. `DashboardSidebar` gained `PortalCompleteViewHref` and a "Complete View" entry appended to the Portal group (mirroring ArcGIS Server's own `Complete View` entry after its per-Root groups). `RegenerationRun.Run` now calls `PortalCompleteViewBuilder.Build` after `PortalSectionBuilder.Build`.

Test coverage: `ByPortalItemRangeTotalsQueryTests` in `Data.Tests` (summing across the date range, one row per Portal item, date-range exclusion, empty-result case). `PortalCompleteViewTests` in `Reports.Tests` drives `RegenerationRun.Run` end-to-end against the ticket-09 harvested fixture: asserts the page exists, the sidebar marks it active, paging is turned on, every row (including the real `+`-containing id) matches an independently computed SQL oracle with the same percent-encoding applied, and the page still renders with a full, non-empty table when the Included Root allow-list is empty. Full suite green: 96 Data.Tests, 164 Domain.Tests, 48 Cli Tests, 57 Reports.Tests, 4 RegressionTests.

Reviewed via `/code-review`: no correctness, reuse, simplification, or efficiency findings against the implementation itself; one process finding (this ticket file not yet closed), addressed by this same edit.
