# 12 — ArcGIS Server section-wide Complete View

**What to build:** A single, uncapped, section-wide sortable table listing every folder/service/type combination across every Included Root (site as a column), with built-in paging so the page stays light regardless of row count.

**Blocked by:** 10 (independent of, and can run in parallel with, ticket 11).

**Status:** done

- [x] New idempotent SQL view (or views) introduced for the per-site, cross-service rollup this table needs — created idempotently, and only from the Reports project's own startup, never the Cli's. *(Superseded — see Comments: no view was needed.)*
- [x] Complete View lists every service under every Included Root, uncapped, with `google.visualization.Table`'s `page: 'enable'` paging turned on.
- [x] Each row is sortable by column header (hits, total time taken, average time taken, successful hits, failed hits).
- [x] Verified against the fixture database at a size representative of the real per-site spread — some Roots with very few services, at least one with many.

## Comments

No SQL view was introduced. `ByArcGisServiceRangeTotalsQuery` (`src/IisLogParserArcGIS.Data/Queries/`) selects directly against `aggregated_by_arcgis_service` with `GROUP BY site, folder, service_name, service_type` over the date range — per ticket 03's resolution, a view is only required when a report's grain collapses across a dimension *finer* than a base table stores. This table's grain (one row per site/folder/service/type) matches the base table's grain exactly; only the date dimension is being summed, the same category ticket 03 already blessed for `ByRootRangeTotalsQuery` (Summary's Complete View). Recording this here as the explicit superseding note ticket 03's own precedent calls for, since this section-wide cross-Root case wasn't one of ticket 03's literal enumerated examples.

`ArcGisServiceRangeTotalRow` carries the four grouping-key fields flat (`Site`/`Folder`/`ServiceName`/`ServiceType`) rather than via `Domain.Aggregation.ArcGisServiceIdentity`, continuing the same pattern already used by the ticket-11 Leaderboard row types.

`GoogleChartsRenderer.RenderTable` gained a `TablePaging` enum parameter (default `Disabled`, so the Summary Complete View's existing behavior is untouched) that turns on `page: 'enable'`/`pageSize: 50` when `Enabled` — used only by this section-wide view, per the ticket. Column-header sorting needed no new code: `google.visualization.Table` sorts by header click by default (ticket 02's superseded decision), the same mechanism already relied on for the Summary Complete View.

`ArcGisServerCompleteViewBuilder` (`src/IisLogParserArcGIS.Reports/Sections/`) mirrors `SummarySectionBuilder`'s shape; wired into `RegenerationRun.Run` after the per-site section builder, and linked from `DashboardSidebar` as a "Complete View" entry under ArcGIS Server.

Test coverage: `ByArcGisServiceRangeTotalsQueryTests` (summing across dates, one row per site/folder/service/type combination, Included-Root filtering, date-range exclusion, empty/null-argument cases) in `Data.Tests`; `ArcGisServerCompleteViewTests` in `Reports.Tests` drives `RegenerationRun.Run` end-to-end against the ticket-09 harvested fixture and asserts the page exists, the sidebar marks it active, paging is turned on, every row matches an independently computed SQL oracle, the unlisted `proxy` Root never appears, and — using `WorkingDatabaseCopy` to seed 60 services under `titan` and 1 under `rhea` — a realistic per-site spread (some Roots with very few services, one with many) renders correctly. Full suite green: 164 Domain.Tests, 74 Data.Tests, 48 Cli Tests, 4 RegressionTests, 42 Reports.Tests (332 total).

Reviewed via `/code-review` against `HEAD`: 0 hard Standards violations (2 minor pre-existing-pattern smells noted, not introduced by this change); 1 Spec finding (the no-view decision above, now documented). No scope creep found.
