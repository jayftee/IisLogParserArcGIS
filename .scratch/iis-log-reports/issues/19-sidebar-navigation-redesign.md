# 19 — Collapsible, palette-driven sidebar navigation

**What to build:** The Dashboard's sidebar (ticket 01) only made ArcGIS Server's per-site groups collapsible - Summary, Portal, and every metric sub-group (`Successful Requests`, `Leaderboard: Successful Hits`, etc.) rendered as a permanently-expanded flat list, while the site's own operator judged the result "a treeview from the 1990s": inconsistent collapsing, and a group header (`<span>`) styled identically to its sub-group headers, with no way to tell depth or hierarchy apart except indentation.

**Status:** done

- [x] Every group in the sidebar tree - not just ArcGIS Server's per-site groups - now renders as a native `<details>` disclosure, auto-expanded only along the path to the current page. `SidebarNode.Collapsible` is gone; collapsibility is now implied by having `Children`, since there's no longer a case where it's ever `false`.
- [x] Sibling groups at the same level share one `<details name="...">` value (the ancestor-title path down to their shared parent), so opening one sibling closes the others via the browser's own native "exclusive accordion" behavior - no JavaScript, and the sidebar never grows past one open branch per level regardless of how many ArcGIS Server sites/metric groups exist.
- [x] Depth-based typography (uppercase top-level labels, shrinking size/weight per level) is gone - every label is the same weight/size now, matching what actually read well in the prototype. Hierarchy comes from indentation and state color alone, not font styling.
- [x] The disclosure chevron moved from a left-side marker to a right-aligned `::after` pseudo-element (`justify-content: space-between` on the row), so every chevron lines up at the same right edge regardless of nesting depth, and rotates on open.
- [x] New color system pulled from the approved brand palette (Stone/Sky/Dawn/Sunset/Prairie/Pasture) instead of ad-hoc hex values: `--text-default` (Stone Mid `#545860`) for normal labels, `--text-highlight` (Sky Mid `#0077cd`) for the open-branch/active-leaf text - chosen specifically for contrast against `--text-default`, not just against the background, after an early attempt (reusing the header's own navy as both bg and highlight text) proved too close in hue/lightness to read as "different." `--highlight-bg` (Sky at 12% alpha) is shared by hover and the active leaf's persistent background - hover never changes font color, only background; the active state adds the text color on top of that same background.
- [x] The header background is now literally the same value as the hover/highlight background (one shared CSS variable), with its text recolored to the old solid navy (`--brand-dark`, Sky Dark `#002c4e`) so it stays legible against the now-much-lighter header bar.
- [x] Brand text changed from "IIS Log Reports" to "ArcGIS IIS Log Reports".
- [x] `PageShellRenderer`'s sidebar-rendering methods now thread a `SidebarRenderContext` record struct (`ActiveHref` + `RootPrefix`) instead of two loose string parameters, to stay under this repo's 3-argument analyzer limit once the accordion `name`-grouping needed a third (`parentKey`) parameter.

## Comments

Built and iterated as a throwaway static-HTML prototype first (`/prototype`), separate from the real generator, so the operator could click through a full 245-page tree (real 14 `IncludedRoots` site names, real section/metric labels) and compare three structurally different approaches side by side before any real code changed:

- **A - Accordion Tree** (the winner): nested `<details>`, single-open-per-level accordion, custom chevrons, depth-based typography.
- **B - Rail + Panel**: a slim rail for the 4 top-level sections; clicking one swapped a side panel.
- **C - Flat Tree + Filter**: top-level sections always expanded, with a live filter box on the 14-site ArcGIS Server branch.

Verdict: **A**, refined over several rounds against real screenshots of Docusaurus's own sidebar as a reference target (right-aligned chevrons, uniform bold labels, a font-color change reserved for the open/active branch rather than hover) and the operator's approved brand palette (for the header/hover/highlight color system). B and C were explicitly dropped rather than further refined.

The full three-variant prototype (all working code, including the two abandoned variants) is preserved on the `prototype/sidebar-nav-ui` branch, out of `main` - see `.scratch/iis-log-reports/prototypes/sidebar-nav/` there for the generator script and a README explaining how to regenerate and click through it. `main` carries only the validated Variant A decision, folded into `PageShellRenderer.cs`, `PageShellAssets.cs`, `SidebarNode.cs`, and `DashboardSidebar.cs`.

Solution builds clean, 0 warnings. `Domain.Tests` (164), `Data.Tests` (121), and `Tests` (56) all pass unaffected. `Reports.Tests`: 43 pass; the other 68 fail only because `HarvestedAggregateDatabaseFixture` still can't build in this environment for the same pre-existing, unrelated reason documented in ticket 18's Comments (a `LocalTimeZone: America/Edmonton` date-window gap at the tail of the fixture's corpus, which worsens as real calendar time passes) - confirmed via a throwaway smoke test that called `PageShellRenderer.Render` directly (no corpus fixture needed) and inspected the real output: correct `<details name>` accordion grouping at every level, `is-open`/`active` classes landing on the right nodes, correct relative-link depth, and the new brand text - then deleted.
