# 17 — Flat Leaderboards: IP, Referer, URI, User Agent

**What to build:** Four standalone Top-500-by-hits Leaderboard pages (IP, Referer, URI, User Agent), each with All/Last-7-Days variants, capped at 500 rows — unlike the two uncapped Complete Views (tickets 12, 15).

**Blocked by:** 10.

**Status:** done

- [x] Each of the four pages lists up to 500 entities ranked by hits descending, sourced directly from its existing flat aggregate table — no new SQL view needed.
- [x] A dimension with fewer than 500 distinct values (any of them, at fixture scale) renders with only as many rows as exist — no placeholder rows.
- [x] Sortable by column header via `google.visualization.Table`.
- [x] Referer's leaderboard excludes the `-` placeholder value per the existing aggregation rules (already enforced upstream — verify it's respected here, not re-implemented).

## Comments

Implemented as a new `FlatLeaderboardSectionBuilder` (`src/IisLogParserArcGIS.Reports/Sections/`), wired into `RegenerationRun.Run` after the ArcGIS Server section, and filling in the sidebar's previously-placeholder "Leaderboard" top-level entry (ticket 10) with 4 dimension groups × All/Last-7-Days.

Four new `Data/Queries/` classes (`ByForwardedForIpTopHitsQuery`, `ByRefererTopHitsQuery`, `ByUriTopHitsQuery`, `ByUserAgentTopHitsQuery`), each selecting directly against its own flat aggregate table with `GROUP BY <dimension> ORDER BY SUM(hits) DESC LIMIT 500` — no SQL view, per ticket 03's resolution (these four were explicitly named in that ticket's own answer as base-table-grain cases). All four share one row shape, `FlatLeaderboardRow` (Value/Hits/TotalTimeTakenSecond), the same same-shape-different-source-column precedent ticket 11 already established for `ArcGisServiceHitsLeaderboardRow`. `ByRefererTopHitsQuery` adds `AND referer <> '-'` to exclude the empty-referer placeholder that `RefererNormalizer` already produces upstream at aggregation time (ticket 09) — the query filters that existing placeholder out; it does not re-derive or re-normalize it.

Rendered via `GoogleChartsRenderer.RenderTable` with `TablePaging.Enabled` (per this ticket's AC: a sortable `google.visualization.Table`, not a `BarChart` like the Portal/ArcGIS Server Top-50 Leaderboards) - `PlainText` cell content, since none of the four dimensions link to a Detail Page.

Test coverage: `By*TopHitsQueryTests.cs` (one per dimension) in `Data.Tests` — summing/grouping, descending order, date-range exclusion, empty-result case, the referer `-` exclusion, and (one per dimension) a 505-distinct-value case proving the `LIMIT 500` cap returns exactly 500 rows with no placeholder padding. `FlatLeaderboardSectionTests` in `Reports.Tests` drives `RegenerationRun.Run` end-to-end against the ticket-09 harvested fixture: asserts all 8 pages exist and mark their own sidebar entry active, paging is turned on, three dimensions' full row sets match an independently computed SQL oracle (capped at 500 the same way), the referer oracle match additionally excludes `-`, a dedicated test confirms `-` never appears as a rendered row even though the real fixture contains it, and a `WorkingDatabaseCopy`-seeded 505-distinct-IP case confirms the cap and descending rank end-to-end. Full suite green: 121 Data.Tests, 164 Domain.Tests, 48 Cli Tests, 86 Reports.Tests.

Surfaced and fixed a pre-existing-pattern test-helper bug while writing the user-agent oracle test: the `");"`-substring-search technique other Complete View tests use to locate the end of an embedded Google Charts JSON array breaks when a cell value itself contains that literal substring (real user-agent strings commonly do). `FlatLeaderboardSectionTests`'s own `ExtractDataRows` uses a bracket/string-aware scanner instead; not backported to the other test files since none of their data (service/root/portal-item identifiers) is likely to contain `");"`, and doing so was out of this ticket's scope.
