# 22 — Survey123 section: Top-50 leaderboard, Complete View, per-device Detail Pages

**What to build:** A new top-level **Survey123** group in the Dashboard's sidebar, placed after **Fieldmaps** and before **Leaderboard**, reporting on the `aggregated_by_survey123_device` table introduced by `iis-log-parser` ticket 21. It is **the Fieldmaps section (reports ticket 21) again, over the Survey123 table**: same three page types, same rules, same structure — so the operator can see who is actually using the Survey123 licences they handed out (and, by absence, who is not). Usage by device/user, hits and time taken; no success/failure dimension.

Read `21-fieldmaps-section.md` first, including its Comments and its follow-up (paging was turned **off** on the Fieldmaps Complete View after the operator's review — this ticket starts from the shipped behaviour, not the ticket's original "paged" wording). This ticket only lists what changes.

**Blocked by:** `iis-log-parser` ticket 21 (the table and its data); reports ticket 21 (done — this ticket reuses its builders, queries and test patterns).

**Status:** done

## Sidebar

```
Summary
Portal
ArcGIS Server
Fieldmaps
Survey123                       ← new
  Leaderboard: Hits
    All
    Last 7 Days
  Complete View
Leaderboard
```

- Header text **Survey123**, as the operator wrote it; the same spelling is used for slugs and paths (`survey123/…`).
- Renders **unconditionally**, like Portal and Fieldmaps: no Included Root applies (the table has no Root column).
- Paths: `survey123/leaderboard-hits/all.html`, `survey123/leaderboard-hits/last-7-days.html`, `survey123/complete-view.html`, `survey123/detail/{deviceId}.html`. `DashboardSidebar` gains the matching slug/href constants; the Detail Page href is computed but **never added to `BuildTree`**.
- Device ids here are lowercase 32-hex strings with no hyphens (unlike Field Maps' hyphenated GUIDs) — still trivially safe as a file-name segment, no escaping concern.

## Pages — identical to Fieldmaps, over the Survey123 table

1. **Leaderboard: Hits** — Top-50 `BarChart`, one bar per **device** (a user with two devices appears twice), All (calendar-year-to-date) and Last 7 Days as separate static pages, same "No data for this range." card, no manufactured tie-break. Bar label: `username · <complete device id>` when attributed, the complete device id alone when not. The ids are 32 characters here (vs 36) so the shared `window.__barChartLeftMargin` sizing has more room; still verify no label is ellipsis-truncated at a typical window width.
2. **Complete View** — one uncapped, column-sortable table, **paging off** (`TablePaging.Disabled`, as Fieldmaps shipped), one row per device, all days merged, year-to-date: Username (blank when unattributed), Device ID, Hits, Total Time Taken (s), Average Time Taken (s) (`SUM(time)/SUM(hits)`), Last Seen (`MAX(local_date)`, ISO `yyyy-MM-dd` text, sorts chronologically), Detail Page link. Every device is listed, attributed or not. A device with several usernames shows one, chosen by the parser ticket's rule (earliest `local_date`, ties alphabetical) — the same shared username expression as the leaderboard, so they can never disagree. Username and device-id cells are HTML-encoded `{v, f}` cells (the table runs with `allowHtml`; a username comes from a request URI and is attacker-influenced).
3. **Per-device Detail Page** — reached only from the Complete View, one file per device, a hits-per-day `AnnotatedTimeLine` with the in-page All / Last 7 Days toggle (both ranges embedded, second drawn on first click from a `<template>`), sparse-data rule (no zero-padding), the device's username or "Unattributed" and its device id in the chart title card, a Back link to the Complete View. Uses the shared `DetailRangeToggle`.

## Data layer

Per ticket 03 (extend `AggregateQueryBase`, no rollup table, no view): the same three queries as Fieldmaps, against `aggregated_by_survey123_device` — Top-50 hits per device over a range, range totals per device (hits, total time, `MAX(local_date)`), daily hit totals for one device — with the shared username-per-device sub-select on whichever queries return a username. A database without the table (or with it empty) must still render the section with empty-data cards / an empty table, via `AggregateDatabaseSchema.TableExists` as Fieldmaps does.

## Implementation guidance (not prescriptive)

Reports ticket 21 built three Fieldmaps-specific builders, three Fieldmaps-specific queries, their row records and `FieldMapsDeviceUsernameSql`, all of which differ from what Survey123 needs only in the table name, labels, slugs, ids and element ids. Copy-pasting the lot is the wrong answer; **parameterize over the source** (table name, section title/slug, chart-id prefix) so both sections share one implementation, keeping the Fieldmaps output and tests unchanged. That refactor is in scope for this ticket if it is the cleanest way to get there; the acceptance criteria below are behavioural and hold either way. The Fieldmaps queries' SQL is the reference for the username selection; do not re-derive it.

## Acceptance criteria

- [x] A "Survey123" group appears in the sidebar after Fieldmaps and before Leaderboard, containing `Leaderboard: Hits` (All, Last 7 Days) and `Complete View`; no entry ever links to a Detail Page. The group renders even with an empty Included Root allow-list, and the Fieldmaps group is unchanged.
- [x] Top-50 pages render a `BarChart` of at most 50 devices, descending by hits, labelled per the rule above; fewer than 50 devices renders fewer bars with no placeholder bars; an empty range renders the "No data for this range." card. All / Last-7-Days ranges match independently computed SQL oracles (the oracle reads `aggregated_by_survey123_device`, never the Field Maps table).
- [x] Top-50 bar labels carry the complete, untruncated device id, and none is ellipsis-truncated by Google Charts at a typical window width (verified in a real browser, not reasoned about).
- [x] Complete View lists every device in the table (attributed and unattributed), one row each, with the seven columns above (including Last Seen), **paging off**, sortable (Last Seen sorts chronologically); rows match an independent SQL oracle (`SUM` hits/time per device, average = `SUM(time)/SUM(hits)`, `MAX(local_date)` for Last Seen).
- [x] A Detail Page exists for every device the Complete View lists, is linked only from the Complete View, and has the All / Last-7-Days in-page toggle with exactly one file per device.
- [x] A device with hits on only two widely separated dates shows exactly those two dates in its chart — no zero-padded days between them (fixture-injected).
- [x] A device with two distinct usernames shows one, per the earliest-date-then-alphabetical rule, identically on the leaderboard and Complete View (fixture-injected; the real corpus has four such devices once harvested).
- [x] With no rows in `aggregated_by_survey123_device` (or the table absent from an older database), the section still renders with empty-data cards / an empty table instead of crashing.
- [x] Survey123 data never appears in the Fieldmaps pages and vice versa (a test that both sections coexist over one database and each shows only its own devices).
- [x] Fieldmaps' pages and tests are unchanged by this ticket (or only mechanically adjusted if the builders are parameterized).
- [x] `CONTEXT.md` updated: **Detail Page** and **Leaderboard** cover a Survey123 Device too; the **Survey123 Device** term from parser ticket 21 is used consistently (not "phone", "handset").
- [x] `README.md`'s Reports/section descriptions, site-map bullet, Section Builder count and mermaid diagram mention the Survey123 section.
- [x] Rendered pages (the three page types, at least one with real-looking data — the full-year corpus backfill gives ≈221 devices — and one with the empty state) are shown to the operator and approved **before** any CSS/layout/chart-styling change is committed. Table/chart content and wiring may be committed independently.
- [x] Full solution builds clean with 0 warnings and the test suites pass.

## Out of scope

- Comparing usage against the list of Survey123 licences handed out (done elsewhere, outside this project).
- Per-user (rather than per-device) grouping: by request, everything here is per device.
- Combining Fieldmaps and Survey123 into one section or one cross-app "who uses what" view.
- Changing how Survey123 devices are detected or attributed — that is `iis-log-parser` ticket 21.

## Decisions (carried over from Fieldmaps, confirmed by the operator there)

1. One Detail Page per device, reached from the Complete View, All / Last 7 Days via an in-page toggle.
2. Top 50 ranked by hits; unattributed devices included in both views.
3. All and Last 7 Days as separate Top-50 pages.
4. "All" is calendar-year-to-date over that year's database.
5. Last Seen column on the Complete View.
6. Top-50 bar labels show the complete device id.
7. Paging off on the Complete View (added after Fieldmaps' review).

## Comments
Implemented in ca45603 by parameterizing the Fieldmaps implementation over its source table rather than copying it. **Data:** `ByFieldMapsDevice{TopHits,RangeTotals,DetailDailyHitTotals}Query` became `DeviceTopHitsQuery`, `DeviceRangeTotalsQuery` and `DeviceDetailDailyHitTotalsQuery`, each taking the by-device table name in its constructor (one of the `AggregateTableNames` constants); the row records and the shared username sub-select became `DeviceHitsLeaderboardRow`, `DeviceRangeTotalRow`, `DeviceDailyHitTotalRow` and `DeviceUsernameSql.Expression(tableName)`. The username SQL is unchanged, so leaderboard and Complete View still cannot disagree. **Reports:** a `DeviceSection` descriptor (title, slug, table; `Fieldmaps` and `Survey123` instances) is passed to `DeviceLeaderboardSectionBuilder`, `DeviceCompleteViewBuilder` and `DeviceDetailPageBuilder`, which `RegenerationRun` calls once per section. Element ids keep the `fieldmaps-…` prefix for Fieldmaps and use `survey123-…` for Survey123. `DashboardSidebar` gained `Survey123SectionSlug`/`Survey123CompleteViewHref`, the shared `DevicePageHref`/`DeviceCompleteViewHref`/`DeviceDetailHref`, `Survey123DeviceDetailHref` and a Survey123 node between Fieldmaps and Leaderboard (Detail Page href never in `BuildTree`). The Fieldmaps output is byte-for-byte the same and the existing Fieldmaps Reports tests pass unmodified; the Data query tests were only mechanically adjusted to the new constructors. `CONTEXT.md` (Detail Page, Leaderboard) and `README.md` (site map, Section Builder count 8 → 11, mermaid diagram) updated. No CSS, layout or chart-styling change was made.

Tests: `DeviceQueriesSourceTableTests` (Data: each query reads only its own table), and `Survey123SectionTests`, `Survey123CompleteViewTests`, `Survey123DeviceDetailPageTests` (Reports, 25 tests: independent SQL oracles over `aggregated_by_survey123_device`, per-device labels, sparse two-date device, two-username device, HTML-injection username, empty and absent-table cases, and Fieldmaps + Survey123 coexisting over one database each showing only its own devices). Full solution: 0 warnings; Domain 250, Host 62, Data 165, Regression 4, Reports 159, all passing.

Real-browser check: regenerated a Dashboard from the corpus database plus 40 harvested days (61 Survey123 devices) and drove headless Chrome at 1400x900. All 50 Top-50 bar labels carry the complete 32-char id (`username · id`, or the bare id), none ellipsis-truncated; the Complete View shows all seven columns, unpaged; a Detail Page shows the toggle, Back link and `Survey123 » Unattributed » <id>` title. Last 7 Days rendered the "No data for this range." empty card (the corpus ends 2026-09-10, before the run date), which doubles as the empty-state sample.

**Left open:** the operator's own visual review of the rendered pages (the one unchecked acceptance criterion). Nothing visual is gated on it since no styling changed, but the operator hasn't seen them yet. Screenshots from the check above were not committed.

Operator visual review: the operator validated the rendered Fieldmaps and Survey123 output from the full-year (253-date) backfill of the 2026 corpus and confirmed it is fine, which closes the last open acceptance criterion. Nothing visual changed in the follow-up tickets 23-28 either (a regenerated Dashboard is byte-identical to `ca45603`'s, per ticket 23).
