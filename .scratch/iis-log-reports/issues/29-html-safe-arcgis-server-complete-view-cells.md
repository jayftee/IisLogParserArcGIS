# 29 — HTML-encode the ArcGIS Server Complete View's Site, Folder and Service Type cells

**What to build:** Three Dashboard tables are drawn with Google Charts' `allowHtml`, which treats every plain string cell as markup. The Device Complete Views wrap attacker-influenced text in an HTML-safe `{ v, f }` cell (`HtmlSafeText`, added with the Device sections), and the Portal table encodes its link, but the **ArcGIS Server Complete View** added `Site`, `Folder` and `ServiceType` as bare strings. `Folder` is a URL path segment lowercased by `ArcGisServiceIdentityParser` and `ServiceType` is any segment that merely ends in `Server`, so a client requesting `/arcgis/rest/services/<img src=x onerror=alert(1)>/svc/MapServer` (or a type such as `<svg/onload=alert(1)>Server`) had that markup handed to every viewer's browser as HTML when they opened the page (a stored-XSS vector on the Dashboard, driven by anonymous request URLs; the test proves the cells were raw strings, it does not execute the payload in a browser). Found by the code review that followed `iis-log-parser` tickets 22-24; fixed alongside its tickets 25 and 26.

**Blocked by:** none.

**Status:** done

## Change

- New `Rendering/HtmlSafeTableCell.Create(string?)` (internal): the `{ v, f }` cell with `v` the raw text (the table sorts on it) and `f` its HTML-encoded form (the table displays it); `null` becomes blank. It is the previous private `DeviceCompleteViewBuilder.HtmlSafeText`, moved so every `allowHtml` table has one implementation.
- `ArcGisServerCompleteViewBuilder` uses it for `Site`, `Folder` and `ServiceType` (`Site` only ever comes from the operator's `IncludedRoots`, so it is defense in depth). `DeviceCompleteViewBuilder` switches to the shared helper with no change in output.
- The service-name and Details cells were already encoded and are unchanged; the Portal table needed no change.

## Acceptance criteria

- [x] A service row whose folder is `<img src=x onerror=alert(1)>` and whose type is `<svg/onload=alert(1)>Server` renders `Folder`/`ServiceType`/`Site` as `{ v, f }` cells: `v` holds the raw text, `f` contains no `<`.
- [x] The existing oracle tests (every service across every included root) still pass, reading cells through `CellText`.
- [x] Device Complete View output and tests unchanged (all `CompleteView` tests pass, 41).
- [x] Build with 0 warnings; full suite passes.

## Out of scope

- The other tables: only three use `allowHtml` (ArcGIS Server, Device, Portal) and the other two were already safe. Leaderboards and charts do not use `allowHtml`.
- Making an unsafe `allowHtml` cell impossible to add by construction (e.g. a typed cell builder); the doc comment on `HtmlSafeTableCell` states the rule.

## Comments

Tests first: `ArcGisServerCompleteViewTests.Run_CompleteView_EscapesAttackerInfluencedFolderAndServiceType_SoTheyCannotInjectMarkup` failed with all three cells still plain strings, then passed. The first draft of the test put markup in `Site` as well; that failed for an unrelated reason (a markup-laden site becomes a detail-page directory name) and is not realistic since sites come from the operator's `IncludedRoots` allow-list, so the test keeps `titan` and asserts its cell is encoded anyway. `FindRow` in that test class now reads cells with `DeviceSectionTestSupport.CellText`, which accepts a plain string or a `{ v, f }` cell.
