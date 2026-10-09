# 30 — Complete View TSV export, and table paging returns now that it exists

**What to build:** Two related changes to every Complete View page (Summary, Portal, ArcGIS Server, Fieldmaps, Survey123 — `DashboardSidebar`/`DeviceSection.All`):

1. Each Complete View's `.html` page gains a `.tsv` sibling file (same name, same directory, `.tsv` extension) holding the exact same rows as plain tab-separated text, plus a visible "Download as TSV" link on the page itself.
2. `google.visualization.Table` paging, turned off for ArcGIS Server/Portal in ticket 20 ("the operator wanted every row visible without clicking through pages, at the cost of the DOM-size guard paging existed for"), comes back for all five Complete Views at page size 1000 — now that the TSV exists as the "see everything" escape valve, the DOM-size guard can return without losing the "see every row" ability ticket 20 asked for. The flat Leaderboard tables (`FlatLeaderboardSectionBuilder`) stay at their existing page size of 50, unaffected.

Recorded in [ADR-0010](../../../docs/adr/0010-complete-view-paging-returns-now-that-tsv-exists.md); the glossary gained a dedicated "Complete View" entry in [CONTEXT.md](../../../CONTEXT.md) covering both.

**Blocked by:** none.

**Status:** done

## Change

### TSV export

- New `Rendering/TsvWriter` (name TBD during implementation): given the exact same `List<IReadOnlyList<object?>>` table each Complete View builder already assembles for `GoogleChartsRenderer.RenderTable`, plus a count of trailing link-only columns to drop, writes a `.tsv` file:
  - Unwraps any `{v, f}`-shaped cell to its plain `v` (every identifier column: Portal Item id, Service Name, Username, Device Id, and the HTML-safe Site/Folder/Service Type cells from ticket 29).
  - Drops the given number of trailing columns (the "Detail Page" link column on Portal/ArcGIS Server/Device Complete Views; none on Summary's).
  - Reuses the already-rounded numeric values (`Math.Round(x, 3)`) exactly as shown in the HTML table — no separate precision.
  - Replaces any embedded TAB/CR/LF in a cell's text with a single space (the folder/service-type/portal-item-id/username/device-id columns are parsed from untrusted request URIs and can carry raw control characters below 0x20, per `DetailPagePathSegment`'s own doc comment — a literal tab or newline would otherwise corrupt the TSV's row/column structure).
  - Writes UTF-8 (no BOM), LF line endings, tab-joined, one row per line, via the same output-directory-relative path join `StaticPageWriter` uses — so the file lands in the same Regeneration Run output tree and rides the existing build-to-temporary-location/deploy-on-success mechanism (ticket 27 Part 3) automatically, with no special-casing needed.
- Each of `SummarySectionBuilder`, `PortalCompleteViewBuilder`, `ArcGisServerCompleteViewBuilder`, `DeviceCompleteViewBuilder` calls the writer with its own already-built table and its own output-relative `.tsv` path (same as its `.html` path with the extension swapped).
- Each Complete View's page body gains a small "Download as TSV" link next to its table, pointing at the sibling file's relative path.

### Paging

- `GoogleChartsRenderer.RenderTable`'s page size becomes a parameter (defaulting to the existing 50) instead of a single shared `private const`, so the flat Leaderboard tables (the only other `TablePaging.Enabled` caller) keep their current page size unchanged.
- `SummarySectionBuilder`, `PortalCompleteViewBuilder`, `ArcGisServerCompleteViewBuilder`, `DeviceCompleteViewBuilder` switch from `TablePaging.Disabled` to `TablePaging.Enabled`, passing a page size of 1000.

## Acceptance criteria

- [x] All five Complete Views (Summary, Portal, ArcGIS Server, Fieldmaps, Survey123) produce a `.tsv` file at the same output-relative path as their `.html` page, extension swapped.
- [x] A TSV's header row and column order matches its HTML table's, minus any trailing link-only column.
- [x] A TSV's data rows match the HTML table's rows exactly for every non-link column (plain identifier value, same rounded numeric figures).
- [x] A row whose folder/service-type/portal-item-id/username/device-id contains a raw control character (tab, CR, or LF) writes as a single space in the TSV instead of corrupting the row/column structure; the same row's HTML cell is unaffected (still HTML-encoded per ticket 29).
- [x] A row whose folder/service-type/portal-item-id/username/device-id starts with `=`, `+`, `-`, or `@` (CSV/TSV formula injection, CWE-1236) is prefixed with a single quote in the TSV so a spreadsheet opening it treats the text as literal, never a formula; a numeric cell's own leading `-` (a negative number) is untouched.
- [x] Every Complete View page shows a "Download as TSV" link whose href resolves to the sibling file.
- [x] All five Complete View tables render with `page: 'enable'` and `pageSize: 1000`; a Complete View with more than 1000 rows exposes pager controls, one with fewer does not lose any rows.
- [x] The flat Leaderboard tables (IP, referer, URI, user agent) are unchanged: still `page: 'enable'`, `pageSize: 50`.
- [x] Build with 0 warnings; full suite passes (Domain, Host, Data, Regression, Reports).

## Out of scope

- Any UI/sidebar entry pointing at the TSV beyond the one link on its own Complete View page.
- Making the TSV path configurable, or gating TSV generation behind a settings flag — always generated alongside the HTML page, like every other Dashboard file.
- Revisiting page size for the flat Leaderboard tables.

## Comments

Written during a `grill-with-docs` session with the operator (2026-10-09). Facts gathered while grilling: all five Complete View builders already build an HTML-cell-shaped `List<IReadOnlyList<object?>>` table before handing it to `GoogleChartsRenderer.RenderTable` (`PortalCompleteViewBuilder`, `ArcGisServerCompleteViewBuilder`, `DeviceCompleteViewBuilder`, `SummarySectionBuilder.BuildCompleteViewBody`); `DetailPagePathSegment`'s doc comment confirms folder/service-name/service-type/portal-item-id values are parsed straight from untrusted request URIs and can carry any ASCII control character below 0x20 (ticket 29 already found and fixed the HTML-injection half of the same root cause); ticket 20's own log recorded turning ArcGIS Server/Portal Complete View paging off, and the flat Leaderboard tables being deliberately left on at page size 50 — confirmed via `git log` that no later commit touched that decision, so it's exactly as ticket 20 left it today.

Decisions made during grilling (see ADR-0010 and the new "Complete View" CONTEXT.md entry for the paging/TSV rationale): all five Complete Views in scope, including future `DeviceSection.All` entries; link-only columns dropped from the TSV entirely rather than kept as a path or external URL; numeric values reused at the HTML table's existing rounding rather than recomputed at full precision; control characters replaced with a space rather than CSV-style quoted or left unguarded; the HTML/TSV table data is shared (reusing the same table object) rather than rebuilt independently for each output; UTF-8 no BOM / LF chosen over a BOM+CRLF Excel-friendly variant, matching how `StaticPageWriter` already writes every HTML page; TSV headers reuse the HTML table's exact column text rather than a separate machine-friendly naming scheme; page size is parameterized rather than bumping the shared constant, specifically to avoid also changing the flat Leaderboard tables' page size, which the operator did not ask for.

### Implementation

Built as speced: `Rendering/TsvWriter` (unwraps `{v,f}` cells via duck-typed reflection for the Portal/ArcGIS Server anonymous-type link cells and `IReadOnlyDictionary<string,string>` for `HtmlSafeTableCell`'s dictionary cells, so no existing cell-construction code had to change), `GoogleChartsRenderer.RenderTable` gained a `pageSize` parameter defaulting to the pre-existing 50 (the flat Leaderboard tables never pass their own value, so they're untouched by construction, not by convention), and a new `Rendering/CompleteViewBody` composes the "write TSV sibling + download link + paged table" sequence once for all four builders to call (added during `/code-review`, which flagged the sequence as duplicated near-verbatim four times).

`/code-review` (medium) also found a real gap in the first draft: `TsvWriter.FormatCell` neutralized TAB/CR/LF but not the leading `=`/`+`/`-`/`@` characters Excel/LibreOffice/Google Sheets treat as formula triggers when a `.tsv` is opened (CSV/TSV formula injection, CWE-1236) - the same attacker-influenced-URI column class ticket 29 already fixed the HTML-injection half of. Fixed by prefixing a string-typed cell's text with a single quote when it starts with one of those characters (the standard mitigation); a numeric cell's own leading `-` (an ordinary negative number) is left alone by construction, since the check only applies to string-typed values.

Full suite green throughout (Domain, Data, Cli, Regression, Reports - 202 Reports.Tests), 0 warnings.
