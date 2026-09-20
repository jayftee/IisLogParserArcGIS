# 14 — Portal metrics and Top-50 leaderboards

**What to build:** Portal's own successful/failed request-count evolution and its Top-50 Successful/Failed-Hits Leaderboards, each with All/Last-7-Days variants — shown unconditionally, regardless of the Included Root allow-list.

**Blocked by:** 10.

**Status:** done

- [x] Portal's section renders even if the Included Root allow-list is empty or doesn't mention Portal's own web-adaptor Root — Portal's inclusion does not depend on that list.
- [x] Both Top-50 Leaderboards render as `BarChart`s, sorted descending by their own measure, with no manufactured secondary tie-break.
- [x] Figures computed via the existing `PortalWebAdaptorName`-scoped aggregate table, with no new Root-filtering logic added to it.

## Comments

Implemented in a Data-layer/Reports-layer split mirroring ticket 11's ArcGIS Server section, scaled down to Portal's simpler shape (no per-Root loop, no average-time split - `aggregated_by_portal_item` has only one combined `time_taken_second` column, not the successful/failed split ticket 11 added to the ArcGIS Server table).

New `src/IisLogParserArcGIS.Data/Queries/` classes: `ByPortalItemDailyHitTotalsQuery` (`GROUP BY local_date`, summed across every Portal item, no site/Root filter - the aggregate table's rows are already scoped to the configured `PortalWebAdaptorName` at ingest time, per ticket 04's resolution) for the two evolution charts, and `ByPortalItemTopSuccessfulHitsQuery`/`ByPortalItemTopFailedHitsQuery` (each excluding zero-hit-of-that-outcome rows, matching `ByArcGisServiceTop*Query`'s own exclusion) for the two Leaderboards - sharing `PortalItemHitsLeaderboardRow`.

New `src/IisLogParserArcGIS.Reports/Sections/` classes: `PortalSectionBuilder` (built 8 pages: 2 evolution metrics + 2 Leaderboards, each All/Last-7-Days), `PortalBuildContext` (bundles the per-run connection/boundaries/settings, mirroring `ArcGisServerBuildContext`), `PortalChartBodies` (reuses the existing `RequestOutcome` enum and `GoogleChartsRenderer.RenderAnnotatedTimeLine`), `PortalLeaderboardBodies` (labels each bar with the raw `portal_item_id` - per ticket 05's resolution, no title resolution is attempted). `DashboardSidebar` gained `PortalSectionSlug`/`Portal*Slug` constants and `PortalPageHref`, and the Portal sidebar entry (previously a placeholder per ticket 10) now expands into its 4 metric groups. `RegenerationRun.Run` now calls `PortalSectionBuilder.Build` after Summary.

Test coverage: `ByPortalItemDailyHitTotalsQueryTests`/`ByPortalItemTopSuccessfulHitsQueryTests`/`ByPortalItemTopFailedHitsQueryTests` in `Data.Tests` (date-range filtering, summing across items, zero-hit exclusion, empty-result cases). `PortalSectionTests` in `Reports.Tests` drives `RegenerationRun.Run` end-to-end against the ticket-09 harvested fixture: asserts every Portal page exists, that the section still renders with an empty Included Root allow-list (and appears in the sidebar unconditionally), that the successful-requests evolution chart matches an independently computed per-day SQL oracle, and that both Leaderboards' embedded bar order matches an independently computed `ORDER BY ... DESC` oracle. Full suite green: 92 Data.Tests, 164 Domain.Tests, 48 Cli Tests, 52 Reports.Tests, 4 RegressionTests.

Reviewed via `/code-review`. One finding raised: the Last-7-Days evolution chart is built by filtering the year-to-date query's own rows in memory rather than querying the true trailing-7-day range, so during the first few days of a new year it can miss real prior-December data (a shape already present, pre-ticket, in `SummarySectionBuilder`/`ArcGisServerSectionBuilder`, which this ticket's `PortalSectionBuilder` mirrors rather than introduces). Discussed with the user and deliberately left as-is: functionally indistinguishable from the unavoidable case where a "Last 7 Days" window simply has less than 7 days of data behind it (e.g. shortly after logging begins) - not something a query change can fix when the underlying data window is genuinely short.
