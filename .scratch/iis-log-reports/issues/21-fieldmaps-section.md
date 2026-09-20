# 21 — Fieldmaps section: Top-50 leaderboard, Complete View, per-device Detail Pages

**What to build:** A new top-level **Fieldmaps** group in the Dashboard's sidebar, placed after **ArcGIS Server** and before **Leaderboard**, reporting on the `aggregated_by_field_maps_device` table introduced by `iis-log-parser` ticket 20. It exists so the operator can see who is actually using the Field Maps licences they handed out (and, by absence, who is not), so the reports are about **usage by device/user** — hits and time taken. There is no success/failure dimension (the table has none).

**Blocked by:** `iis-log-parser` ticket 20 (the table and its data), reports tickets 10/14/15/16/19/20 (all done — this ticket reuses their shared rendering, sidebar and Detail Page patterns).

**Status:** done

## Sidebar

```
Summary
Portal
ArcGIS Server
Fieldmaps                       ← new
  Leaderboard: Hits
    All
    Last 7 Days
  Complete View
Leaderboard
```

- The header text is **Fieldmaps**, as the operator wrote it (the product is "Field Maps"; the sidebar label follows the operator's request, and the same spelling is used for slugs, `fieldmaps/…`).
- Like Portal, the section renders **unconditionally**: no Included Root applies (the table has no Root column), so the Included Root allow-list neither gates nor filters it.
- Page paths (relative, same conventions as Portal): `fieldmaps/leaderboard-hits/all.html`, `fieldmaps/leaderboard-hits/last-7-days.html`, `fieldmaps/complete-view.html`, `fieldmaps/detail/{deviceId}.html`. `DashboardSidebar` gains the matching slug/href constants; the Detail Page href is computed but, as with tickets 13/16, **never added to `BuildTree`** — no sidebar entry ever points at a Detail Page. Device ids are casefolded GUIDs (hex and hyphens only), so unlike Portal item ids they are always safe as a file-name segment — no escaping concern.

## Pages

### 1. Leaderboard: Hits — Top 50, bar chart

A `BarChart` (same `RenderBarChart` and card/title treatment as the Portal and ArcGIS Server Top-50 Leaderboards, ticket 20 — including the "No data for this range." card for an empty result) of the 50 devices with the most hits, descending by hits, no manufactured tie-break (ticket 06). One bar per **device**, not per user: a user with two devices can appear twice, by request.

- **All** = calendar-year-to-date over the database (per the map's time-range rule); **Last 7 Days** = trailing 7 days. Separate static pages per range, not an in-page toggle, matching every other Leaderboard.
- **Bar label:** the username when the row is attributed, followed by the **complete** device id (the full casefolded GUID, not truncated), so two devices of one user stay distinguishable — e.g. `user00364@somewhere · 227c3d43-ea74-4d05-aaca-1e47ac4bcde9`. Unattributed devices (username `NULL`) are labelled by their full device id alone. The labels are long, so the bar chart relies on the shared `window.__barChartLeftMargin` helper (ticket 20), which sizes the label column to the widest label, capped at half the container width; verify that no label is ellipsis-truncated at a typical window width.

### 2. Complete View — every device, all days merged

One uncapped, column-sortable `google.visualization.Table` (same shape as the ArcGIS Server and Portal Complete Views, tickets 12/15) with one row per **device**, all days merged (`GROUP BY device_id`, year-to-date):

| Column | Notes |
|---|---|
| Username | Blank for a device that is still unattributed. |
| Device ID | The casefolded GUID. |
| Hits | `SUM(hits)` |
| Total Time Taken (s) | `SUM(time_taken_second)`; three decimals, per ticket 20's "(s)" column rule. |
| Average Time Taken (s) | `SUM(time)/SUM(hits)`; three decimals. |
| Last Seen | `MAX(local_date)` — the most recent Local Date on which the device had any Field Maps hit. Rendered as a date and must sort chronologically (a Google Charts `date` column, or ISO `yyyy-MM-dd` text, which also sorts correctly). Requested by the operator as the key column for licence validation: a device last seen months ago is not really using its licence. |
| Detail Page | Link to that device's Detail Page (same column pattern as ticket 16). |

Every row of the table is listed — attributed or not — because this is the operator's "entire table" view, and sorting on Username lets them group or set aside the unattributed devices themselves.

If a device's rows carry more than one non-`NULL` username (a lent device), show one, chosen by the same rule as `iis-log-parser` ticket 20's stage 2: earliest `local_date`, ties alphabetical.

### 3. Per-device Detail Page — line chart, All / Last 7 Days

Reached **only** from the Complete View's Detail Page column, like the ArcGIS Server service and Portal Item Detail Pages (tickets 13/16); one static page per device in the Complete View. It shows one **line chart of that device's hits per day** (a per-day value, never cumulative — ticket 03), with the same **in-page All / Last 7 Days toggle** (both ranges embedded, second chart drawn on first click from a `<template>`, so no Google Chart is ever drawn into a hidden zero-size container) and the same sparse-data rule (the chart spans only the device's real first-to-last date range, no zero-padding — ticket 06).

The page also shows, above the chart, the device's username (or "Unattributed") and device id, so the operator knows whose device they are looking at.

`AnnotatedTimeLine` for the line chart, per the map's chart-type decision for single-series time charts.

## Data layer

Per ticket 03 (extend `AggregateQueryBase`, no new project, no rollup table, no SQL view — the table's own grain, (date, device), already matches every query; one query per variant, year-to-date boundary and the trailing-7-day boundary come from the run's existing clock/boundaries):

- Top-50 hits per device over a date range (`ORDER BY SUM(hits) DESC LIMIT 50`).
- Range totals per device over a date range (hits, total time, and `MAX(local_date)` as Last Seen) — the Complete View's rows and the Detail Page driver set.
- Daily hit totals for one device over a date range — the Detail Page's chart series.

Add the shared username-per-device selection (earliest `local_date`, then alphabetical, among non-`NULL` rows) to whichever queries return a username, so the leaderboard and Complete View can never show different usernames for the same device.

## Acceptance criteria

- [x] A "Fieldmaps" group appears in the sidebar after ArcGIS Server and before Leaderboard, containing `Leaderboard: Hits` (All, Last 7 Days) and `Complete View`; no entry ever links to a Detail Page. The group renders even with an empty Included Root allow-list.
- [x] Top-50 pages render a `BarChart` of at most 50 devices, descending by hits, labelled per the label rule above; fewer than 50 devices renders fewer bars with no placeholder bars; an empty range renders the "No data for this range." card. All/Last-7-Days ranges match independently computed SQL oracles.
- [x] Top-50 bar labels carry the full, untruncated device id, and none is ellipsis-truncated by Google Charts at a typical window width.
- [x] Complete View lists every device in the table (attributed and unattributed), one row each, with the seven columns above (including Last Seen), paging off (every row shown, like the other Complete Views), sortable (Last Seen sorts chronologically); rows match an independent SQL oracle (`SUM` hits/time per device, average = `SUM(time)/SUM(hits)`, `MAX(local_date)` for Last Seen).
- [x] A Detail Page exists for every device the Complete View lists, is linked only from the Complete View, and has the All/Last-7-Days in-page toggle with exactly one file per device (proving the toggle stays in-page).
- [x] A device with hits on only two widely separated dates shows exactly those two dates in its chart — no zero-padded days between them (fixture-injected, like ticket 16).
- [x] A device with two distinct usernames shows one, per the earliest-date-then-alphabetical rule, identically on the leaderboard and Complete View.
- [x] With no rows in `aggregated_by_field_maps_device` (or the table absent from an older database), the section still renders with empty-data cards / an empty table instead of crashing.
- [x] `CONTEXT.md` updated: **Detail Page** covers a Field Maps device too; **Leaderboard** lists device among its dimensions; the **Field Maps Device** term from ticket 20 is used consistently (not "phone", "handset").
- [x] `README.md`'s Reports/section descriptions and site map mention the Fieldmaps section.
- [x] Rendered pages (the three page types, at least one of them with real-looking data and one with the empty state) are shown to the operator and approved **before** any CSS/layout/chart-styling change is committed. Table/chart content and wiring may be committed independently.
- [x] Full solution builds clean with 0 warnings and the test suites pass.

## Out of scope

- Comparing usage against the list of licences that were handed out. That comparison is done elsewhere, outside this project; the Dashboard only reports what the logs contain.
- Any per-user (rather than per-device) grouping: by request, everything here is per device. A per-user rollup is a `GROUP BY username` away in the same table if wanted later.
- Changing how `NULL`-username devices are attributed — that is `iis-log-parser` ticket 20.

## Decisions (confirmed by the operator)

1. **One Detail Page per device**, reached from the Complete View, showing All and Last 7 Days of usage via an in-page toggle (not an inline sparkline in the table row).
2. **Top 50 ranked by hits.** Unattributed devices are included in both views.
3. **All and Last 7 Days as separate Top-50 pages**, exactly like the Portal Leaderboard.
4. **"All" is calendar-year-to-date** over that year's database; logs are kept per year, so there is no cross-year view.
5. **Last Seen column** added to the Complete View.
6. **Top-50 bar labels show the complete device id**, not a truncated suffix.

## Comments
Implemented in 54eb5b1 across Data, Reports and docs. **Data:** `ByFieldMapsDeviceTopHitsQuery`, `ByFieldMapsDeviceRangeTotalsQuery` and `ByFieldMapsDeviceDetailDailyHitTotalsQuery` (each extending `AggregateQueryBase`, with row records `FieldMapsDeviceHitsLeaderboardRow`, `FieldMapsDeviceRangeTotalRow`, `ByFieldMapsDeviceDailyHitTotalRow`), all sharing one username sub-select, `FieldMapsDeviceUsernameSql` (earliest `local_date`, ties alphabetical, looked up across the whole table so All and Last 7 Days agree), plus `AggregateDatabaseSchema.TableExists` so a database predating the table renders empty-data cards instead of failing. **Reports:** `FieldmapsSectionBuilder` (Top-50 bar-chart pages), `FieldmapsCompleteViewBuilder` (paged table; Last Seen is ISO `yyyy-MM-dd` text, which sorts chronologically; username/device-id cells are `{v, f}` HTML-encoded because the table runs with `allowHtml` and a username comes from a request URI), `FieldmapsDeviceDetailPageBuilder` (hits per day, `AnnotatedTimeLine`, username or "Unattributed" and device id in the chart title card), `FieldmapsDeviceLabel`, and `DashboardSidebar` constants/hrefs/tree node (Detail Page href never in `BuildTree`). Extracted the All/Last 7 Days toggle markup that Portal's and ArcGIS Server's Detail Pages each duplicated into `Rendering/DetailRangeToggle` and pointed all three at it (output unchanged; existing tests still pass). No CSS, layout or chart-styling change was made.

Tests: 3 Data query test classes plus a `TableExists` test; 3 Reports test classes (`FieldmapsSectionTests`, `FieldmapsCompleteViewTests`, `FieldmapsDeviceDetailPageTests`) driving `RegenerationRun.Run` against the harvested fixture with independent SQL oracles, fixture-injected edge cases (sparse two-date device, two-username device, HTML-injection username) and empty/absent-table cases. Full solution builds with 0 warnings; Domain 215, Host 60, Data 148, Regression 4, Reports 134, all passing.

Spot-check: regenerated a real Dashboard from 17 harvested corpus days (29 devices) and drove headless Chrome over it at 1400x900: the Top-50 bars carry full 36-char device ids with the attributed `user00386@somewhere · <id>` bar un-truncated, the Complete View shows all seven columns, and a Detail Page shows the toggle, Back link and the "Unattributed » <id>" title.

**Left open:** the operator's own visual review of the rendered pages (the one unchecked acceptance criterion above). Nothing visual is gated on it since no styling changed, but the operator hasn't seen them yet.

Follow-up after the operator reviewed the real 429-device Dashboard: paging turned **off** on the Fieldmaps Complete View (`TablePaging.Disabled`), matching the ArcGIS Server and Portal Complete Views (ticket 20), so every device shows without clicking through pages. This supersedes the "paged" wording in the ticket body above.

Operator visual review (recorded later, alongside reports ticket 22): the operator validated the rendered Fieldmaps output from the full-year backfill of the 2026 corpus and confirmed it is fine, which closes the last open acceptance criterion left in this ticket.
