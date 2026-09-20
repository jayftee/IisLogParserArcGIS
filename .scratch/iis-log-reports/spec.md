Status: ready-for-agent

# Reports project: Dashboard reporting layer

## Problem Statement

The aggregate database already captures everything about how the ArcGIS Enterprise system (14 sites — ArcGIS plus 13 solar-system-moon-named Roots — and their Portal) is used: hits, response times, success/failure, per URI/root/service/Portal Item/referer/IP/user agent, one row per day. But none of it is visible to anyone. There's no way for the user, or the clients and executives they answer to, to see usage trends, spot top or slow consumers, or notice failures, without hand-querying the aggregate database directly. The user previously relied on a different tool (`CGI.ArcGIS.Server.StatisticsAnalyzer.Console`) for exactly this kind of visibility and wants an equivalent, matched to how this system's own aggregate data is shaped.

## Solution

A new **Reports** project, alongside the existing Cli/Domain/Data projects in the same solution, that regenerates a complete static-HTML **Dashboard** from the aggregate database every time new data lands — chained onto the existing daily CLI import, always a full rebuild, no incremental updates. The Dashboard is browsable with nothing more than a file viewer or a plain web server (relative links throughout), needs no live database connection or authentication to view, and matches the chart types and table conventions of the user's prior tool. It covers a global cross-Root summary, a per-included-site ArcGIS Server section, a Portal section, and flat cross-cutting leaderboards (by IP, referer, URI, user agent) — down to a Detail Page for every individual service and Portal Item.

## User Stories

1. As a Dashboard viewer, I want a Summary page showing total requests evolving per Root over the current calendar year, so I can see traffic trends across the whole system at a glance.
2. As a Dashboard viewer, I want a Summary table of hits/total time taken/average time taken per Root, sortable by any column, so I can compare Roots directly.
3. As a Dashboard viewer, I want a Portal section showing successful and failed request evolution for Portal as a whole, so I can see Portal's health over the year.
4. As a Dashboard viewer, I want Top-50 Leaderboards of Portal Items by successful hits, failed hits, so I can immediately see the most (and most-failing) used content.
5. As a Dashboard viewer, I want a Complete View listing every Portal Item (hits, total/average time, successful/failed hits), sortable by any column, so I can find any item even if it isn't in a top-50 list.
6. As a Dashboard viewer, I want each Portal Item row/id to link to that item's live page on Portal, so I can see its actual current title and content without the Dashboard needing to track title changes itself.
7. As a Dashboard viewer, I want a Detail Page for any individual Portal Item showing its own successful/failed request evolution over the year, so I can investigate one item in isolation.
8. As a Dashboard viewer, I want an ArcGIS Server group per included site (Root), each showing successful/failed request evolution and successful/failed average-processing-time evolution, so I can monitor each site's health and performance independently.
9. As a Dashboard viewer, I want Top-50 Leaderboards per site by successful hits, failed hits, average time (successful), and average time (failed), so I can spot the busiest and slowest services on that site.
10. As a Dashboard viewer, I want a single section-wide Complete View listing every folder/service/type combination across every site (site as a column), sortable by any column, so I can find or compare any service regardless of which site it's on.
11. As a Dashboard viewer, I want a Detail Page for any individual service showing its own request-volume and average-processing-time evolution (successful and failed), so I can investigate one service in isolation.
12. As a Dashboard viewer, I want every per-metric page to offer both an "All" (year-to-date) and a "Last 7 Days" view as separate destinations, so I can switch between long-term trend and recent activity without losing my place.
13. As a Dashboard viewer, I want four flat Leaderboard pages (top 500 by hits: IP, referer, URI, user agent), so I can see the biggest cross-cutting consumers regardless of which site or item they hit.
14. As a Dashboard viewer, I want a persistent sidebar covering every section, so I can navigate directly to any report without hunting through nested pages.
15. As a Dashboard viewer, I want the site's home page and the sidebar brand to land me on Summary → Successful Requests → All, so I always have a sensible, predictable default landing page.
16. As a Dashboard viewer, I want every page to show when the Dashboard was generated, so I know how current the data I'm looking at is.
17. As a Dashboard viewer, I want charts to show only the real dates an entity actually has data for — no invented zeros before it existed or after it stopped — so the chart reflects reality rather than a manufactured full year.
18. As a Dashboard viewer, I want a Leaderboard or Complete View with fewer entities than its nominal size to just show what exists, with no placeholder rows, so the page reads cleanly regardless of how much data is behind it.
19. As a GIS administrator, I want the Reports project to only ever show Roots I've explicitly included, so that scanner/pentest noise and infrastructure traffic I haven't reviewed never appears in a client-facing report.
20. As a GIS administrator, I want to maintain the Included Root list as simple configuration, with no separate friendly-display-name bookkeeping, so upkeep stays low.
21. As a GIS administrator, I want Portal's own section to always appear regardless of the Included Root list, so I don't have to separately manage Portal's inclusion on top of the ArcGIS Server allow-list.
22. As a GIS administrator, I want the ArcGIS resource proxy Root excluded from every report, so its forwarded calls don't get double-counted against the real destination service's own numbers.
23. As a GIS administrator, I want the Dashboard to regenerate automatically as part of the existing daily import, so I never have to remember a separate manual step to keep it current.
24. As a GIS administrator, I want to point the Reports project at wherever I host the generated files (a shared folder or an IIS site) without changing anything in the generated output itself, so hosting stays entirely my own infrastructure decision.
25. As a developer extending the Reports project, I want its queries to live in the existing Data project's established `AggregateQueryBase` pattern, so reporting queries follow the same conventions as everything else that touches the aggregate database.
26. As a developer extending the Reports project, I want year-to-date boundaries computed once per Regeneration Run rather than re-derived per query, so every page generated in one run agrees on what "today" and "this year" mean.
27. As a Dashboard viewer looking at a ranked list with tied values, I want the existing order to stand without a manufactured secondary sort, so the report doesn't pretend to a precision the underlying data doesn't have.

## Implementation Decisions

**Project shape**: new `IisLogParserArcGIS.Reports` project alongside Cli/Domain/Data. Its own entry point/orchestration triggers immediately after the existing daily CLI import (no independent schedule) and drives one full Regeneration Run: read the current aggregate database, compute every figure, and emit the entire static file tree, replacing whatever was generated previously. Reports' own configuration (`appsettings.json`) holds the Included Root allow-list and a new `PortalBaseUrl` setting; it does not duplicate the Cli's existing `PortalWebAdaptorName`.

**Query/data layer** (lives in the existing `IisLogParserArcGIS.Data` project, not a new one): concrete `*Query` classes subclass the existing `AggregateQueryBase`. No precomputed rollup/summary table — every figure is computed by on-the-fly SQL each Regeneration Run. New idempotent SQL views (`CREATE VIEW IF NOT EXISTS`, via a new schema class co-located with the existing `AggregateDatabaseSchema`, created only from the Reports entry point, never the Cli's) are used only where a report's grain requires collapsing across a dimension finer than a base table stores (a per-site total summed across folder/service/type; Portal-global summed across every Portal Item) — anywhere a base table's row grain already matches what's needed, Query classes select directly against it.

- Every line-chart point is a per-day value, never cumulative: hit-count series use `SUM(hits)`/`SUM(successful_hits)`/`SUM(failed_hits)` for that Local Date; average-time series use a fresh `SUM(time_taken_second)/SUM(hits)` for that Local Date alone.
- Every total/Leaderboard figure uses `SUM(time_taken_second)/SUM(hits)` over the full calendar-year-to-date range. Each Leaderboard/table variant is its own query (e.g. top-50-by-successful-hits is a separate query from top-50-by-average-time-failed) rather than one query parameterized by measure.
- Root-level ("Summary") charts carry no successful/failed split — a single total-requests-per-Root line chart, sourced from the existing by-root aggregate's plain `hits` column. This is a deliberate narrowing of the literal report inventory in `requirements.md` (superseded there with a note): Root-level reporting is about traffic volume, not diagnosing failures, and the by-root aggregate has no success/fail columns to split on anyway.
- No secondary tie-breaking sort anywhere. `google.visualization.Table` views are viewer-sortable by column header; `BarChart` Top-50 views already sort by their primary ranking measure, and an unresolved tie among adjacent, near-identical bars isn't misleading.
- Sparse-data entities: a line-chart series spans only the entity's real date range in the aggregate data — no zero-padding before its first appearance or after its last.
- Under-full Leaderboards/Complete Views: render however many rows/bars actually exist, no placeholder for unfilled slots (the common case for small Roots — some carry only a handful of services).
- **Year-to-date boundary**: computed once, in .NET, at the start of the Regeneration Run — never in SQL (`date('now')` is UTC and blind to the app's configured local time zone that Local Date already depends on, per the existing `LocalUtcOffsetResolver`). Use the standard .NET `TimeProvider` abstraction (not a bespoke interface) injected into the Reports entry point — `TimeProvider.System` in production, a fixed one in tests — combined with the existing local-time-zone resolution to derive `today`/`yearStart` once and thread both through every query call made during that run.

**Included Root configuration**: a flat array of bare, lowercase Root values (the 14 real names: `arcgis`, `charon`, `deimos`, `europa`, `galatea`, `iapetus`, `kerberos`, `mimas`, `namaka`, `oberon`, `phobos`, `rhea`, `titan`, `umbriel`) in the Reports project's `appsettings.json`. Gates the ArcGIS Server section only. Portal is shown unconditionally — its aggregate rows carry no Root column and were already correctly scoped at ingest time via the existing `PortalWebAdaptorName` setting. The `proxy` Root (a real, legitimate ArcGIS resource-proxy path observed in the corpus) is deliberately excluded — it forwards to the real destination service, which is already counted under its own Root, so including `proxy` too would double-count those calls. No friendly-display-name field. Any Root present in the aggregate database but absent from the allow-list is silently excluded from every report; the data itself remains directly queryable for fringe cases.

**Portal item display naming**: no title resolution against the live Portal REST API — a title can change at any time, so any resolved value would need re-fetching every run anyway, gaining nothing over just accepting the raw id. Every Portal Item reference (Leaderboards, Complete View, Detail Pages) shows the raw `portal_item_id`, rendered as a link to that item's live page on Portal (`https://<PortalBaseUrl>/home/item.html?id=<id>`) built from the new `PortalBaseUrl` setting — no network call or credentials needed at generation time.

**Site map, navigation, page generation, and charting** (validated via a click-through prototype, `.scratch/iis-log-reports/prototype-dashboard/`, branch `prototype/page-generation-and-site-map`, not merged — carry its `common.js` client-side module over close to verbatim):

- Persistent sidebar, no "Home" entry: the sidebar brand and `index.html` both resolve to Summary → Successful Requests → All.
- **Summary**: Successful/Failed Requests (All/Last-7-Days leaves), plus one Complete View (per-Root table).
- **Portal**: Successful/Failed Requests and Top-50 Successful/Failed-Hits Leaderboards (All/Last-7-Days leaves), plus one uncapped Complete View (~20,000 rows at real scale) — the only path to a Portal Item Detail Page.
- **ArcGIS Server**: one collapsible group per Included Root, each with its 8 metrics (success/failure counts, success/failure average time, and their 4 Top-50 Leaderboards, All/Last-7-Days leaves), plus one section-wide, uncapped Complete View (~4,000 rows at real scale, site as a column) — the only path to a service Detail Page.
- **Leaderboard**: four flat Top-500 pages (IP, referer, URI, user agent) — capped, unlike the two Complete Views above, since these dimensions have genuinely unbounded cardinality.
- Both uncapped Complete Views ship with `google.visualization.Table`'s built-in `page: 'enable'` paging, so DOM cost stays bounded regardless of row count — completeness doesn't need to be traded away for a problem paging already solves.
- Range (All vs. Last 7 Days) is a navigation-level choice — separate static pages, not an in-page toggle — except on drill-down Detail Pages, which keep an in-page toggle since they aren't bookmarked sidebar destinations.
- Every page carries a static "Created on `<generation timestamp>` / Generated by `<tool>`" footer, baked in at generation time.
- Charting: Google Charts, CDN-loaded. `AnnotatedTimeLine` for every single-series time chart; `LineChart` for Summary's multi-Root overlay only; `BarChart` for every Top-50 Leaderboard; `google.visualization.Table` for every full list.
- Each static page is a shared shell (header/sidebar-mount-point/footer) plus a per-page inline `<script>` that renders the sidebar and draws that page's chart(s)/table from data embedded inline in the page — not fetched from a shared JSON file. The .NET-side string-templating mechanism (raw interpolation vs. a lightweight engine) is left to implementation.

## Testing Decisions

A good test here exercises the Reports project from the outside — real aggregate data in, a real generated file tree out — and asserts on externally observable results (which files exist, what a page's embedded data contains, how many rows a table has), never on the internal call sequence used to produce them.

**Primary seam (the one seam this spec tests against)**: a single top-level Reports entry point — analogous to the Cli's own `Program.cs` — that takes an aggregate-database connection, the Reports configuration (Included Roots, `PortalBaseUrl`), and a `TimeProvider`, and produces the complete static-HTML file tree at a given output directory. Tests call this one seam end-to-end and assert against the resulting files.

**Fixture strategy**: build the test fixture aggregate database from real, already-parsed data rather than hand-typed rows, reusing the existing `IisLogParserArcGIS.RegressionTests` project's own tooling (`TestSupport/CompiledProgram.cs`, `TestSupport/TempOutputDatabase.cs`) — it already runs the shipped Cli executable against the real, checked-in `2026/` corpus for one target date at a time, writing into a given SQLite path. Since a Daily Batch replace only touches its own date's rows, calling this once per date across the corpus's full date range into one shared output database file accumulates a realistic, multi-day, multi-entity aggregate database (real Roots, real service/Portal Item identities, the real `proxy` Root, naturally-occurring under-full per-Root service counts) — built once per test collection (it's expensive: one process invocation per corpus date), not per test. Where a specific edge case isn't naturally present in the harvested data (a guaranteed exact ranking tie, a Portal Item or service that only exists for part of the date range), insert the extra rows directly into a working copy of the harvested database before invoking the Reports entry point, rather than hand-building an entire fixture database from scratch.

Individual `*Query` classes (the Data-project additions from the query/data-layer decisions above) may additionally get their own narrower tests against a seeded SQLite connection, mirroring the existing `tests/IisLogParserArcGIS.Data.Tests` repository/query test style (a real connection, never a mock, per [ADR-0002](../../docs/adr/0002-dapper-repository-and-query-split.md)'s rationale) — but the primary seam for this feature is the one end-to-end entry point above.

## Out of Scope

- Hosting/deployment of the generated files — the user's own existing infrastructure; the Reports project's job ends at producing the file tree.
- Confirming Regeneration Run behavior/performance against real production data volumes (922,000,000 hits/year, ~20,000 Portal Items, ~4,000 services) — needs a real, populated aggregate database, which doesn't exist yet. Revisit once the Reports project and production data both exist.
- Resolving a friendly title for Portal Items via the live Portal REST API — explicitly rejected; titles change too often to make caching worthwhile.
- Authentication or per-viewer filtering of any kind.
- Adding the `proxy` Root (or any other unlisted Root) to the Included Root allow-list.
- A precomputed rollup/summary table for reporting figures — computed on the fly instead.
- Incremental Dashboard updates — every Regeneration Run is a full rebuild.

## Further Notes

- Full decision rationale and the evidence behind each choice above lives in [map.md](map.md) and its seven resolved tickets (`issues/01`–`issues/07`) — this spec is the buildable synthesis, not a replacement for that record.
- The full literal report inventory is [requirements.md](requirements.md); one entry there (the per-Root successful/failed line charts) is explicitly superseded by this spec's Root-level decision above.
- Do not size or test-tune anything off the checked-in `2026/` corpus's raw scale or the `C:\Temp\2026\...` sample pages — both are small, non-representative datasets (the corpus is deliberately capped at 1,000 hits/file for file-discovery testing). Real production is 922,000,000 hits/year, ~20,000 Portal Items, ~4,000 ArcGIS Server services (~2,000 in `arcgis` alone, ~2,000 spread unevenly across the other 13 Roots — e.g. `rhea` ≈ 5, `titan` ≈ 400); use the corpus only for its real entity identities and file-format shape, not for capacity assumptions.
