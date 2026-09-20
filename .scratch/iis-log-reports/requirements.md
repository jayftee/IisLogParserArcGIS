# Reporting requirements (verbatim ask, organized)

Captured from the user's original request (2026-09-12). This is reference material for the map — not itself a ticket, and not restated on the map body.

## Global (across all Included Roots)

- Line graph: evolution of total successful requests, per Root, calendar-year-to-date — all Roots on one chart.
- Line graph: evolution of total failed requests, per Root, calendar-year-to-date — all Roots on one chart.
- Sortable table: hits, total time taken, average time taken, per Root.

  **Superseded** by [ticket 03](issues/03-query-and-rollup-data-layer.md): the two successful/failed line graphs collapse into a single "evolution of total requests, per Root" chart — no success/fail split at the Root level.

## Portal — global

- Line graph: evolution of successful requests to Portal, year-to-date.
- Line graph: evolution of failed requests to Portal, year-to-date.
- Leaderboard (bar chart): top 50 Portal Items by successful hits.
- Leaderboard (bar chart): top 50 Portal Items by failed hits.
- Sortable table: hits, total time taken, average time taken, successful hits, failed hits — per Portal Item.

## Portal — per Portal Item (Detail Page)

- Line graph: evolution of successful requests to this Portal Item, year-to-date.
- Line graph: evolution of failed requests to this Portal Item, year-to-date.

## ArcGIS Server — per site

- Line graph: evolution of successful requests to the site, year-to-date.
- Line graph: evolution of failed requests to the site, year-to-date.
- Leaderboard (bar chart): top 50 folder/service/type by successful hits.
- Leaderboard (bar chart): top 50 folder/service/type by failed hits.
- Line graph: evolution of average processing time for successful requests, year-to-date.
- Line graph: evolution of average processing time for failed requests, year-to-date.
- Leaderboard (bar chart): top 50 folder/service/type by average time taken, successful requests.
- Leaderboard (bar chart): top 50 folder/service/type by average time taken, failed requests.
- Sortable table: hits, total time taken, average time taken, successful hits, failed hits — per folder/service/type.

## ArcGIS Server — per folder/service/type (Detail Page)

- Line graph: evolution of successful requests, year-to-date.
- Line graph: evolution of failed requests, year-to-date.
- Line graph: evolution of average processing time, successful requests, year-to-date.
- Line graph: evolution of average processing time, failed requests, year-to-date.

## Leaderboards (flat, top 500 by hits, standalone pages)

- Forwarded-for IP: ip, hits, time taken (seconds), average time taken (seconds).
- Referer (excluding `-`): referer, hits, time taken (seconds), average time taken (seconds).
- URI: uri, hits, time taken (seconds), average time taken (seconds).
- User agent: user agent, hits, time taken (seconds), average time taken (seconds).

## Settled architecture (see map Notes for the current list; ADRs 0005-0007 for the reversible-decision rationale)
