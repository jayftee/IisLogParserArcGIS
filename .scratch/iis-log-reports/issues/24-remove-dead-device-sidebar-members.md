# 24 — Remove the dead and test-only `DashboardSidebar` device members

**What to build:** Ticket 22 parameterized the device sections over `DeviceSection`, which left some `DashboardSidebar` members with no production caller. Delete them and point the tests that used them at the one path production code actually uses. Review finding: Standards 2 (Speculative Generality / Middle Man).

**Blocked by:** —

**Status:** done

## Members to remove

- `DashboardSidebar.FieldmapsCompleteViewHref` and `DashboardSidebar.Survey123CompleteViewHref` — no references anywhere in the repo; `DeviceSection.CompleteViewHref` / `DashboardSidebar.DeviceCompleteViewHref(sectionSlug)` replaced them. (`Survey123CompleteViewHref` was added new in ticket 22 and never used.)
- `DashboardSidebar.FieldmapsDeviceDetailHref(deviceId)` and `DashboardSidebar.Survey123DeviceDetailHref(deviceId)` — one-line wrappers over `DeviceDetailHref(sectionSlug, deviceId)` that only tests call; `DeviceSection.DetailHref(deviceId)` goes to `DeviceDetailHref` directly.

Keep `FieldmapsSectionSlug`, `Survey123SectionSlug`, `DeviceLeaderboardHitsSlug`, `DevicePageHref`, `DeviceCompleteViewHref` and `DeviceDetailHref`: ticket 22's "DashboardSidebar gains the matching slug/href constants" is satisfied by these, and production code uses them.

Tests that currently call `DashboardSidebar.FieldmapsDeviceDetailHref(id)` / `Survey123DeviceDetailHref(id)` (in `FieldmapsDeviceDetailPageTests`, `FieldmapsCompleteViewTests`, `Survey123DeviceDetailPageTests`, `Survey123CompleteViewTests`) switch to `DeviceSection.Fieldmaps.DetailHref(id)` / `DeviceSection.Survey123.DetailHref(id)`. Mechanical only; no assertion changes.

## Acceptance criteria

- [x] The four members above no longer exist, and `grep` finds no reference to any of them in `src/`, `tests/`, `README.md` or `CONTEXT.md`.
- [x] Every test that used them now uses `DeviceSection.<Section>.DetailHref(...)` with the same assertions.
- [x] No generated page changes (same sidebar, same links, same file paths): the existing Fieldmaps and Survey123 Reports tests pass unmodified apart from the href call.
- [x] Build clean with 0 warnings; Reports tests pass.

## Out of scope

- Anything about where titles/slugs live (ticket 25) or how the tests are organised (tickets 27, 28).

## Comments
Implemented in 714945b. Removed `DashboardSidebar.FieldmapsCompleteViewHref`, `Survey123CompleteViewHref`, `FieldmapsDeviceDetailHref` and `Survey123DeviceDetailHref` (no other production change). The four test classes that called the wrappers (`FieldmapsCompleteViewTests`, `FieldmapsDeviceDetailPageTests`, `Survey123CompleteViewTests`, `Survey123DeviceDetailPageTests`) now call `DeviceSection.Fieldmaps.DetailHref(...)` / `DeviceSection.Survey123.DetailHref(...)` and import `Reports.Sections` instead of `Reports.Rendering`; no assertion changed. `grep` finds no reference to any removed member in `src/`, `tests/`, `README.md` or `CONTEXT.md`. Full solution builds with 0 warnings; Domain 250, Host 62, Data 165, Regression 4, Reports 159, all passing (same counts as before, as expected for a pure refactor).
