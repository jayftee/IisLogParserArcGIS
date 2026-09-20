# 27 — Consolidate the Fieldmaps and Survey123 Reports tests into tests over `DeviceSection`

**What to build:** Ticket 22 shared the production code between the two device sections but copied the tests: `Survey123SectionTests`, `Survey123CompleteViewTests` and `Survey123DeviceDetailPageTests` are near-copies of the three `Fieldmaps*Tests` classes (the Complete View pair differs by about 48 lines), and the builders they exercise are now `DeviceLeaderboardSectionBuilder`, `DeviceCompleteViewBuilder` and `DeviceDetailPageBuilder`, so the test tree no longer mirrors the source tree as ADR 0004 asks ("1:1 class-to-test-class naming, so coverage gaps are visually obvious by comparing the two trees"). Write each test once, over the section, so the tests keep the "cannot drift apart" property the production change has. Review findings: Standards 1 (Reports half), 5, 6 and the Spec reviewer's minor gap.

**Blocked by:** 24, 25 (so the tests are written against the final `DeviceSection.DetailHref` and `DeviceSection.All`)

**Status:** done

## What to do

1. Replace `FieldmapsSectionTests`, `FieldmapsCompleteViewTests`, `FieldmapsDeviceDetailPageTests` and the three `Survey123*Tests` with three classes named for the builders they exercise: `DeviceLeaderboardSectionBuilderTests`, `DeviceCompleteViewBuilderTests`, `DeviceDetailPageBuilderTests`. Each test becomes a `[Theory]` over `DeviceSection.All` (Fieldmaps, Survey123), using the section's own `SectionSlug`, `TableName`/`Table`, `LeaderboardHitsHref` and `DetailHref` instead of hard-coded `fieldmaps/…` / `survey123/…` strings. **Every existing assertion is preserved for both sections** (oracle comparisons, label rule, fewer-than-50 bars, no-data card, absent table, seven columns/paging off, Last Seen ISO sort, links, two-username rule, HTML-injection username, one-page-per-device, back link, toggle, sparse two-date, per-day not cumulative, last-7-days exclusion).
2. Make the section-specific bits data-driven rather than copied: the oracle's device-id parsing takes the id from the label (split on ` · `, or the whole label when unattributed) instead of the hard-coded last-36 / last-32 characters; the fixture-deleting SQL and the SQL oracles take the table name from the section.
3. Rename `FieldmapsTestSupport` to `DeviceSectionTestSupport` (it now serves both sections) and collapse `DeviceRow` / `Survey123DeviceRow` and `WorkingDatabaseCopy.InsertFieldMapsDeviceRows` / `InsertSurvey123DeviceRows` into one section-taking helper, so a row is built and inserted once per section without a copied helper.
4. Keep the tests that are about the two sections together rather than one section: the sidebar-order test (Fieldmaps, Survey123, Leaderboard) and the coexistence test (each section shows only its own devices, including the shared-device-id case) stay, in whichever of the new classes fits best.
5. **Add** one assertion the review found missing: against a database **without** the section's table, no Detail Page files are written for it (the leaderboard and Complete View empty-state assertions already exist).

## Acceptance criteria

- [x] No `Fieldmaps*Tests` or `Survey123*Tests` class remains under `tests/IisLogParserArcGIS.Reports.Tests/`; the three `Device*BuilderTests` classes exist and each behaviour is tested once, parameterised over both sections.
- [x] Per section, the number of behavioural test cases does not decrease (every assertion listed in step 1 still runs for both Fieldmaps and Survey123), plus the new absent-table/no-Detail-Pages assertion for both.
- [x] `FieldmapsTestSupport` no longer exists; no helper is duplicated per section.
- [x] Production code is untouched by this ticket (tests and test-support only).
- [x] Build clean with 0 warnings; the Reports suite passes.

## Out of scope

- Data-layer tests and tests for the small types (ticket 28).
- Any production change.

## Comments
Implemented in da91a7b. The six `Fieldmaps*Tests` / `Survey123*Tests` classes are replaced by `DeviceLeaderboardSectionBuilderTests`, `DeviceCompleteViewBuilderTests` and `DeviceDetailPageBuilderTests`, each behaviour written once as a `[Theory]` over `DeviceSection.All` (keyed by section slug via `DeviceSectionTestSupport.SectionSlugs` / `SectionFor`, so every case is enumerated separately). Everything section-specific comes from the section: hrefs from `LeaderboardHitsHref` / `CompleteViewHref` / `DetailHref`, chart and table ids from `SectionSlug`, SQL oracles and fixture edits from `Table.Name`, and the oracle now takes the device id from the bar label (split on ` · `) instead of the last 36 or 32 characters. `FieldmapsTestSupport` became `DeviceSectionTestSupport` (public, so `MemberData` can use it) with one `DeviceRow` factory returning a new `DeviceTestRow`; `WorkingDatabaseCopy` lost `InsertFieldMapsDeviceRows` / `InsertSurvey123DeviceRows` in favour of one `InsertDeviceRows(section, ...)`, plus `DeleteAllDeviceRows` and `DropDeviceTable`. The two cross-section tests stay as single tests in the leaderboard class: sidebar order (ArcGIS Server, then every section in `DeviceSection.All` order, then Leaderboard, no Detail Page entry, no other section's paths inside a group) and coexistence (each section shows only its own device on its leaderboard, Complete View and Detail Pages). Added, per the review: an absent-table test asserting no Detail Pages directory is written for the missing section (both sections), a Complete View absent-table test, and the shared-device-id Detail Page test now runs for both sections (it was Survey123-only). Production code is untouched. Full solution builds with 0 warnings; Domain 250, Host 62, Data 169, Regression 4, Reports 163 (the 111 other tests plus 52 device cases: every prior assertion runs for both sections, plus the additions above), all passing.
