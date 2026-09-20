Type: prototype
Status: resolved

## Question

How does the Reports project actually turn aggregate-database rows into the Dashboard's static HTML, and what is the exact site map (folder/file layout and URL structure) that ties every page together?

Covers:
- The page-generation mechanism itself: a plain HTML template with placeholder substitution (matching the user's prior .NET/Google-Charts precedent), a lightweight templating engine (e.g. Scriban), or something else — and how shared chrome (nav, header/footer, CSS) is factored so it isn't duplicated across hundreds of Detail Pages.
- The concrete file/folder layout and naming convention for: the home page, the Global section, the ArcGIS Server section (per-site index pages, per-folder/service/type Detail Pages), the Portal section (global index, per-Portal-Item Detail Pages), and the four flat Leaderboard pages — and how every page links to the ones above and below it in the hierarchy (per the map's "one interlinked Dashboard" decision).
- How a Global line graph with all Included Roots on one chart, or a per-site chart with tens of series, stays legible rather than a cluttered mess — this is exactly the kind of thing worth looking at rendered rather than deciding on paper.
- Where Google Charts data gets embedded (inline per-page JS, or shared JSON files fetched by each page).

Build a rough, real sample: a home page, one Global section page, one ArcGIS Server per-site page, and one Detail Page, wired together with real (or realistic fake) data and an actual Google Charts line graph and bar chart — enough for the user to click through and react to the navigation, layout, and chart legibility.

## Answer

Resolved through several rounds of a click-through prototype (`prototype/page-generation-and-site-map` branch, `.scratch/iis-log-reports/prototype-dashboard/`, built with a Node.js `generate.js` standing in for the eventual .NET generation step). Final decisions:

**Navigation is a persistent sidebar, not a header/breadcrumb** (two other nav-chrome variants — breadcrumb+tabs, and a breadcrumb+lateral-jump hybrid — were prototyped and rejected in favor of a sidebar, matching the user's own review of comparable dashboards). Structure:

- **Summary**: Successful Requests / Failed Requests, each expandable into **All** and **Last 7 Days** leaves; plus a single Complete View (per-Root totals table).
- **Portal**: Successful/Failed Requests and Top 50 Successful/Failed Hits, each with All/Last-7-Days leaves; plus a single Top 500 Hits table (all Portal Items). Portal Item Detail Pages are reached only by clicking a row in Top 500 Hits.
- **ArcGIS Server**: one collapsible group per Included Root (the real 14: ArcGIS, Charon, Deimos, Europa, Galatea, Iapetus, Kerberos, Mimas, Namaka, Oberon, Phobos, Rhea, Titan, Umbriel — see the note added to ticket 04), each expanding into its 8 metrics (success/failure counts, success/failure avg time, and the 4 corresponding Top-50 Leaderboards), each with All/Last-7-Days leaves; plus a single section-wide **Complete View** — one sortable table of every folder/service/type across every site (site as a column). **Complete View is the only path to a per-service Detail Page** — no other page links to one.
- **Leaderboard**: Top 500 Hits for IP / URL / User Agent / Referer, each with All/Last-7-Days leaves.
- **No "Home" entry** in the sidebar. The sidebar brand/logo, and the site's `index.html`, both resolve to Summary → Successful Requests → All (the default landing page on first load) rather than a placeholder splash.
- Range (**All** vs **Last 7 Days**) is a navigation-level choice — two separate static pages/files per metric — not an in-page toggle, *except* on the per-service and per-Portal-Item Detail Pages (reached by drill-down click, not sidebar), which keep an in-page All/Last-7-Days toggle since they aren't bookmarked sidebar destinations.

**Chart types match the user's prior Google-Charts implementation** (found at `C:\Temp\2026\ServiceAverageResponse\*.htm` and `C:\Temp\2026\ServiceUsage\*.htm` — a `CGI.ArcGIS.Server.StatisticsAnalyzer.Console` tool), not the initial prototype's plain LineChart/hand-rolled table:
- **`AnnotatedTimeLine`** for every single-series time chart (site/Portal/service/item metrics) — its built-in zoom/pan range selector is what the user meant by "allows for zooming in time."
- **`LineChart`** (multi-series) stays for Summary only (all Included Roots overlaid on one chart) — confirmed already fine as-is.
- **`BarChart`**, styled per the old tool (40%-width chart area, axis titles/gridlines, startup animation), for every Top-50 Leaderboard.
- **`google.visualization.Table`** (native column-header sort) for every full list: Summary Complete View, Portal Top 500, ArcGIS Server Complete View, and the four flat Leaderboards. **This supersedes ticket 02's hand-rolled-script recommendation** — see the note added there.
- All four packages (`corechart`, `bar`, `table`, `annotatedtimeline`) load via the standard `google.charts.load` + `setOnLoadCallback` on every chart-bearing page.

**Page-generation mechanism**: each static page is a shared HTML shell (header/sidebar-mount-point/footer) plus a per-page inline `<script>` block that (a) calls a shared client-side module — the prototype's `common.js`, which the real Reports project would carry over close to verbatim — to render the sidebar and draw whatever chart(s)/table this page needs, and (b) embeds this page's own data inline as JS, not fetched from a shared JSON file. The .NET side's job per Regeneration Run is exactly what `generate.js` does here: enumerate sites/metrics/ranges, and for each, emit one file with the right inline data and the right sidebar-highlight metadata. The exact .NET-side string-templating mechanism (raw interpolation vs. a lightweight engine like Scriban) is a low-stakes implementation choice, not decided here — deferred to implementation, along with all label/style/spacing polish (e.g. the bar chart's item-label column width), per the user's own call that those are implementation-time tweaks.

**Every page carries a generation-timestamp footer** — "Created on `<timestamp>`" / "Generated by `<tool name>`" — static text baked in at Regeneration Run time (not a live client clock), matching the old tool's exact footer convention, so a printed page shows when its data was current.

**Access rule confirmed**: a service's Detail Page is reachable only from ArcGIS Server → Complete View; nothing else links to one.

Prototype captured on branch `prototype/page-generation-and-site-map` (not merged to main) as the primary source; this ticket and the map's Decisions-so-far hold the validated answer.

## Amendment (2026-09-13, while resolving ticket 05)

**Portal's item-listing table becomes an uncapped Complete View**, not the "Top 500 Hits" table described above — mirroring the ArcGIS Server section's own Complete View (every folder/service/type, no cap). Rationale: the original 500-row cap assumed a page carrying more than one report; now that the site map settled on one report per page (see the Answer above), the legibility concern behind the cap no longer applies. Scoped to this one table only — the Portal section's Top-50 Successful/Failed Hits bar-chart leaderboards stay capped at 50, and the four flat standalone Leaderboard pages (IP/Referer/URI/User-Agent) stay capped at 500, since those have genuinely unbounded cardinality where a full table wouldn't be usable.

## Amendment (2026-09-13, while resolving ticket 06) — firms up the amendment above

The sample report pages this whole map's chart/UX decisions were checked against are from a small demo system — real production is far larger: 922,000,000 hits in 2025, ~20,000 distinct Portal Items, and ~4,000 distinct ArcGIS Server services (~2,000 in the `arcgis` Root alone, ~2,000 spread unevenly across the other 13 Roots — e.g. `rhea` ≈ 5, `titan` ≈ 400). This replaces the "try full, fall back to 500/5000 if unworkable" provisional plan above with a settled decision, since the real row counts are now known upfront rather than needing empirical discovery: **both Complete Views (Portal's ~20,000 rows, and the ArcGIS Server section-wide one's ~4,000 rows) ship uncapped, with `google.visualization.Table`'s built-in `page: 'enable'` paging turned on.** Paging solves the actual cost (rendered DOM size), so completeness doesn't need to be traded away for a problem paging already handles. The ArcGIS Server Complete View being uncapped also matches the user's prior tool, which already runs it uncapped today.
