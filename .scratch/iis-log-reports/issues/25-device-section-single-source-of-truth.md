# 25 — One source of truth for a device section's title, and a loop instead of six `Build` calls

**What to build:** Two related duplications the code review raised around `DeviceSection` (review finding: Standards 4 — Data Clumps / Shotgun Surgery). Fix both so adding a third device section is a one-place change.

**Blocked by:** —

**Status:** done

## 1. The section title lives in two places

`DeviceSection.Title` (`"Fieldmaps"`, `"Survey123"`) and the literals passed to `DashboardSidebar.BuildDeviceNode("Fieldmaps", FieldmapsSectionSlug)` / `BuildDeviceNode("Survey123", Survey123SectionSlug)` are the same pair written twice.

**Direction of the fix matters.** `DashboardSidebar` is in `Rendering/`, which the README describes as the generic, data-agnostic layer, and it is the documented single source of truth for the nav tree and every href. So do **not** make it depend on `Sections/DeviceSection` (that would invert the layering). Instead add `FieldmapsSectionTitle` and `Survey123SectionTitle` constants beside the existing `*SectionSlug` constants in `DashboardSidebar`, and have both `BuildTree` (via `BuildDeviceNode`) and `DeviceSection.Fieldmaps` / `DeviceSection.Survey123` read them. The `"Fieldmaps"` spelling keeps its existing `#pragma warning disable CC0309` justification (the operator's own spelling of the product name).

## 2. `RegenerationRun` repeats three calls per section

`RegenerationRun.Run` calls `DeviceLeaderboardSectionBuilder`, `DeviceCompleteViewBuilder` and `DeviceDetailPageBuilder` once for `DeviceSection.Fieldmaps` and again, line for line, for `DeviceSection.Survey123`. Add `DeviceSection.All` (an `IReadOnlyList<DeviceSection>`, Fieldmaps then Survey123) and loop over it, running the three builders per section.

## Acceptance criteria

- [x] The strings `"Fieldmaps"` and `"Survey123"` as section titles are each written exactly once (as `DashboardSidebar` constants); `DeviceSection.Title` and the sidebar node titles both derive from them.
- [x] `DashboardSidebar` has no reference to `IisLogParserArcGIS.Reports.Sections`.
- [x] `RegenerationRun.Run` contains one loop over `DeviceSection.All` instead of six device-builder calls; `DeviceSection.All` contains exactly Fieldmaps and Survey123.
- [x] No generated page changes, including sidebar order and every file path: existing Reports tests pass unmodified.
- [x] Build clean with 0 warnings; Reports tests pass.

## Out of scope

- Removing the dead constants (ticket 24), the `DeviceAggregateTable` type (ticket 26), any test reorganisation (tickets 27, 28).

## Comments
Implemented in fd02919. `DashboardSidebar` gained `FieldmapsSectionTitle` and `Survey123SectionTitle` (the Fieldmaps one inside the existing CC0309 justification); `BuildTree` and `DeviceSection.Fieldmaps` / `DeviceSection.Survey123` both read them, so each title is written once and `DashboardSidebar` still has no reference to `Reports.Sections`. `DeviceSection.All` (Fieldmaps, then Survey123) is new, and `RegenerationRun.Run` now runs the three device builders in one `foreach` over it instead of six explicit calls (build order per section is unchanged). No generated page changed and no test was modified. Full solution builds with 0 warnings; Domain 250, Host 62, Data 165, Regression 4, Reports 159, all passing.
