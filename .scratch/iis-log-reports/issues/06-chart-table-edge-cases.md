Type: grilling
Status: resolved

## Question

Graduated from the map's "Chart/table edge cases" fog. Three now-sharp sub-questions:

- **Tie-breaking**: when multiple entities tie exactly on a Leaderboard's ranking measure (hits, or average time taken), what secondary sort key breaks the tie — alphabetical by entity identity, or stable/arbitrary order from the underlying query?
- **Sparse-data entities on line charts**: for a calendar-year-to-date line chart, a newly-appeared entity (a service or Portal Item with no traffic before some date this year) has no rows for the days before it existed. Does its series start partway through the x-axis (no leading points at all before its first day of data), or does the chart still span the full Jan-1-to-today x-axis with explicit zero/blank values for the days before the entity existed?
- **Under-full Leaderboards/tables**: when fewer than N entities exist to rank (fewer than 50 for a bar-chart Leaderboard, fewer than 500 for a flat/Complete View table), does the page simply render however many rows exist, or is there a placeholder/message for the missing slots?

## Answer

**Scale correction surfaced while resolving this ticket**: the sample report pages at `C:\Temp\2026\...` are from a small demo system, not representative of real production volume — the real system received 922,000,000 hits in 2025. Real distinct-entity counts: ~20,000 Portal Items; ~2,000 ArcGIS Server services in the `arcgis` Root alone, plus ~2,000 more spread unevenly across the other 13 Roots (e.g. `rhea` ≈ 5, `titan` ≈ 400) — roughly 4,000 services total. This corrects the map's Scale note (was estimated from the checked-in `2026/` corpus, which is itself an unrepresentative small dataset) and firms up ticket 01's Portal Complete View amendment from a provisional "try full, fall back to 500/5000" into a settled decision — see that ticket's second amendment.

**Tie-breaking: no explicit secondary sort, anywhere.** For `google.visualization.Table` views (every Complete View, and the four flat Top-500 Leaderboards), the widget's own native column-header sort means the viewer controls order themselves — an arbitrary/query-order tie costs nothing. For `BarChart` Top-50 Leaderboards (not viewer-resortable), the primary sort (by the ranking measure, descending) already stands; a tie among adjacent bars with near-identical values isn't misleading, so no secondary key is added there either.

**Under-full Leaderboards/tables**: render however many rows/bars actually exist, no placeholder for unfilled slots. Confirmed as the common case, not a corner case — most of the 13 non-`arcgis` Roots (e.g. `rhea` at ~5 services) will routinely under-fill a Top-50 Leaderboard.

**Sparse-data entities on line charts**: a series spans only the real date range where the entity actually has data — no zero-padding at the start for an entity that appeared partway through the year, and, symmetrically, none at the end for one that stopped appearing before today (e.g. a service live February–September). The chart represents reality; it doesn't manufacture data to fill out the axis.
