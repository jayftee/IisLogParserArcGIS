# 23 — Follow-ups from the code review of the Survey123 section (tickets 21/22 device-section refactor)

**What to build:** Nothing user-visible. This is the **umbrella issue** tracking the fixes for the two-axis code review of reports ticket 22 (`git diff 3fcbaad...HEAD`, commits `ca45603` and `dc5e738`). The review found **no defects** and **no hard standards violations**; it raised six judgement-call Standards findings and one minor Spec gap. Each Standards finding is covered by exactly one child ticket below; nothing here changes what any Dashboard page renders.

**Status:** done

## Child tickets (one per task)

| # | Ticket | Covers review finding | Blocked by |
|---|---|---|---|
| 24 | `24-remove-dead-device-sidebar-members.md` | Standards 2 — unused constants and test-only wrappers (Speculative Generality / Middle Man) | — |
| 25 | `25-device-section-single-source-of-truth.md` | Standards 4 — Data Clumps / Shotgun Surgery (title in two places, six repeated `Build` calls) | — |
| 26 | `26-device-aggregate-table-type.md` | Standards 3 — Primitive Obsession (raw table-name string spliced into SQL) | — |
| 27 | `27-consolidate-device-section-report-tests.md` | Standards 1 (Reports half: ADR 0004 mirroring), 5 (duplicated tests), 6 (`FieldmapsTestSupport` name), Spec (absent-table Detail Pages) | 24, 25 |
| 28 | `28-device-data-tests-and-small-type-tests.md` | Standards 1 (Data half + new types without tests), 5 (duplicated Data test helpers), 6 (`DeviceQueriesSourceTableTests` coverage) | 25, 26 |

Suggested order: 24, 25, 26 (production, independent of each other), then 27, 28 (tests, which depend on the final names and constructors).

## Review findings deliberately **not** turned into tasks

- **"Browser verification is claimed in the ticket's Comments but no artefact is committed"** (Spec (a)). A committed screenshot of a generated page would go stale and the check is a one-off manual step; ticket 22's Comments already record what was checked and how. The operator's own visual approval of the rendered pages remains the one open acceptance criterion on ticket 22 and stays there.
- **README "11 Section Builders" nit** (Spec (c)). The README prose already carries a clarifying parenthetical (the three per-device builders run twice); the count is of builder classes, which is what the diagram shows. Left as is.
- **"`DashboardSidebar` should be built from `DeviceSection`"** (part of Standards 4, as the reviewer phrased it). Not adopted as stated: `DashboardSidebar` lives in `Rendering/`, which the README describes as the generic, data-agnostic layer, so making it depend on `Sections/`'s `DeviceSection` would invert that layering. Ticket 25 removes the duplication the other way round (the sidebar stays the single source of truth for titles, slugs and hrefs, as the README already says) instead.

## Acceptance criteria

- [x] Tickets 24–28 are each `done`.
- [x] After the last of them: the full solution builds with 0 warnings, all suites pass, and a regenerated Dashboard is byte-identical in its Fieldmaps and Survey123 pages to one generated from `ca45603` over the same database (these tickets are pure refactors of code and tests).

## Comments
All five child tickets are done: 24 (`714945b`), 25 (`fd02919`), 26 (`bfe95de`), 27 (`da91a7b`), 28 (`e70b20e`). Every review finding is covered by exactly one of them, as mapped in the table above. Nothing outside tickets 24–28 was changed to close this issue, apart from one thing found on the way and fixed in 28: `TempSqliteDatabase.Dispose` used the global `SqliteConnection.ClearAllPools()`, a latent race that made unrelated Data tests fail intermittently once there were more of them; it now clears only its own database's pool.

Verification of the second acceptance criterion: built `ca45603` (the ticket 22 implementation) in a temporary worktree and regenerated a Dashboard from it and from the current build over the same copy of the full-year database (`LocalTimeZone=UTC`), then compared the two output trees after normalising each page's "Generated on ..." footer timestamp. Every HTML page is byte-identical, including all 410 Fieldmaps and 215 Survey123 files. The only two differing files, `assets/dashboard.js` and `assets/site.css`, differ solely in line endings (zero differing lines once CR is ignored, and `PageShellAssets.cs` has not changed since `ca45603`); that comes from the worktree checkout, not from these tickets. Final suite: 0 build warnings; Domain 250, Host 62, Data 192, Regression 4, Reports 181, all passing.
