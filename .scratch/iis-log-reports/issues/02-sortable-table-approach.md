Type: research
Status: resolved

## Question

What lightweight, client-side approach should render the Dashboard's sortable tables (the Global per-Root table, the Portal per-item table, each ArcGIS Server site's per-folder/service/type table, and the four flat Leaderboard tables, up to 500 rows)?

Since the Dashboard is static HTML with no server to query on interaction (per the map's delivery-mode decision), sorting has to happen entirely in the browser. Survey lightweight, no-build-step options that work by including a script tag against static HTML tables (e.g. a small vendored/CDN library like `sorttable.js`, `tablesort`, `List.js`, or a hand-rolled sort-on-click script) suited to tables of up to ~500 rows with no pagination requirement stated. Recommend one, with the trade-offs (bundle size, CDN vs vendored, whether it needs the table pre-rendered with typed data attributes for correct numeric/date sorting).

## Answer

Use a minimal hand-rolled vanilla-JS sort-on-click script (~40-70 lines, zero dependencies), with the page generator emitting a normalized `data-sort` attribute on every sortable `<td>` (numbers as plain digits, dates as ISO `yyyy-mm-dd`) — this turns numeric/date sorting into a trivial comparison instead of a runtime text-guessing problem, and needs no external file to version, host, or go stale, fitting the same drop-in-`<script>`-tag style already used for Google Charts. Tablesort (tristen/tablesort, MIT, ~2.85 KB core, actively maintained, native `data-sort` support) is the recommended fallback if a known library is preferred over an owned script; sorttable.js is unmaintained since 2019 with no trustworthy CDN/npm distribution, List.js requires markup scaffolding beyond a plain `<table>`, and DataTables is oversized for a no-pagination, sort-only need.

Full research with citations: see the `research/sortable-table-approach` branch, `docs/research/sortable-table-approach.md` (not merged to main — that branch is the throwaway home for the research artifact itself).

## Update (superseded)

While resolving ticket 01 (page generation & site map), the user surfaced their prior Google-Charts implementation (`C:\Temp\2026\ServiceAverageResponse\*.htm`, `ServiceUsage\*.htm`), which renders every full list via **`google.visualization.Table`** — a native, already-loaded part of the same `google.charts.load(['table', ...])` call the rest of the Dashboard uses for its charts, with built-in click-to-sort and no extra library or `data-sort` attribute convention needed.

Given the Dashboard is already committed to Google Charts for every other visual, loading one more package from the same source is simpler than a separate sort mechanism (hand-rolled or Tablesort) — **`google.visualization.Table` is now the answer**, validated in the ticket 01 prototype (Summary Complete View, Portal Top 500, ArcGIS Server Complete View, and all four Leaderboards). The hand-rolled-script and Tablesort research above remains a valid comparison of alternatives, just no longer the recommendation to build from.
