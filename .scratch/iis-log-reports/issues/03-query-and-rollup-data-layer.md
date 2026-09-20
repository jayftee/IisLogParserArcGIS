Type: grilling
Status: resolved

## Question

How does the Reports project read the eight existing aggregate tables to produce every number the Dashboard needs, and where does that querying code live?

Covers:
- Whether the Reports project gets its own query layer (new `IisLogParserArcGIS.Reports` project or a `Reports.Data` companion), or extends the existing `IisLogParserArcGIS.Data` project's `AggregateQueryBase`/Dapper pattern (per ADR-0002) with new read queries.
- How "average time taken" is computed consistently everywhere it's asked for: per-Root/per-site/per-item/per-service totals for tables (`sum(time_taken_second) / sum(hits)` across the full calendar-year-to-date range) versus the day-by-day series a line graph needs (is each point a daily average, a daily total, or a running cumulative total — the literal ask says "evolution of total ... requests" for hit-count charts but "evolution of average processing time" for the time charts, so these are two different per-day aggregations, not the same rollup applied twice).
- How top-50/top-500 Leaderboard selection queries are shaped (one query per Leaderboard, ranked by the stated measure — hits, or average time taken — over the full year-to-date range).
- Whether any of this benefits from a precomputed rollup step (e.g. a `reports_*` summary table refreshed each Regeneration Run) versus computing every figure with on-the-fly SQL against the raw daily aggregate rows on every run — relevant given a full year is ~365 rows per entity across low hundreds of entities.

## Answer

**Query layer location**: New concrete `*Query` classes subclassing `AggregateQueryBase`, living in `IisLogParserArcGIS.Data` — no new project. `AggregateQueryBase` (per ADR-0002) exists specifically for this; a separate Reports.Data companion would be indirection with no isolation benefit since Reports is the only consumer.

**Computation strategy**: On-the-fly SQL every Regeneration Run — no precomputed `reports_*` rollup table. Scale (~365 rows/entity × low hundreds of entities) is trivial for SQLite to `GROUP BY`/`SUM` on every full-rebuild run; a rollup table would add write-path complexity (its own repository, its own replace semantics) for a performance problem that doesn't exist at this scale.

**SQL views**: idempotently created (`CREATE VIEW IF NOT EXISTS`) via a new `ReportViewSchema.EnsureCreated`, co-located with `AggregateDatabaseSchema` in the Data project's Schema folder, but invoked only from the Reports project's own Regeneration Run startup — never from the Cli's daily import. Scope views to genuine cross-entity rollups only — where a report's grain requires collapsing across a dimension finer than a base table stores (a per-site total summed across `folder`/`service_name`/`service_type`; Portal-global summed across every `portal_item_id`). Where a base table's row grain already matches what's needed (a single service's or item's Detail Page, the per-Root Global table/leaderboard, the four flat Leaderboards over IP/referer/URI/user-agent), Query classes select directly against the base table — no view.

**Daily-series semantics** (confirmed against the prior tool's own sample Google Charts data at `C:\Temp\2026\ServiceUsage\DFN_requests.htm` and `C:\Temp\2026\ServiceAverageResponse\DFN_average_response.htm` — both series are per-bucket values, never cumulative): every line-chart point is a per-day value. Hit-count charts: `SUM(hits)` / `SUM(successful_hits)` / `SUM(failed_hits)` for that Local Date. Average-time charts: a fresh `SUM(time_taken_second)/SUM(hits)` for that Local Date alone — not the prior day's figure carried forward.

**Totals/leaderboards**: `SUM(time_taken_second)/SUM(hits)` over the full calendar-year-to-date range (same formula as the daily case, wider window) — confirmed against the prior tool's own leaderboard dumps (`DFN_top_average_response.tab`: `1242/91 = 13.65`). One query per Leaderboard/table variant (e.g. separate queries for top-50-by-successful-hits vs top-50-by-average-time-failed), matching the prior tool's own separate ranked-list outputs, rather than one generic query parameterized by measure.

**Year-to-date boundary**: computed in .NET, not SQL. `date('now')` in SQLite is UTC-based and blind to the app's configured local time zone that Local Date depends on (see CONTEXT.md). No `IClock`/`TimeProvider` abstraction exists yet in the codebase — add a small injectable one. `yearStart`/`today` are computed **once at Regeneration Run start** and threaded through every query call that run, so a run can't straddle a date-boundary tick and disagree with itself across pages.

**Root-level charts — scope revision**: on reflection, the Global section's Root-level charts are about traffic volume ("what cluster receives in terms of query"), not diagnosing failures — so the two separate "evolution of successful/failed requests, per Root" line charts (which would have required deriving success/fail from `aggregated_by_arcgis_service` grouped by `site`, undercounting relative to `aggregated_by_root`'s plain hit count whenever non-service traffic exists under a Root) collapse into **one** "evolution of total requests, per Root" chart — plain `SUM(hits)` from `aggregated_by_root`, all Roots on one chart. This drops the success/fail distinction at the Root level entirely; it stays for every finer grain (site, service, Portal Item) where `aggregated_by_arcgis_service`/`aggregated_by_portal_item` already carry it. Recorded as a superseding note in [requirements.md](../requirements.md).
