---
status: accepted
---

# Complete View table paging returns now that a TSV sibling exists

Ticket 20 turned `google.visualization.Table` paging off for the ArcGIS Server and Portal Complete Views (the Device Complete Views followed the same convention when built) so every row stayed visible in the browser without clicking through pages, accepting unbounded DOM cost as the trade-off. Reports ticket 30 gives every Complete View a `.tsv` sibling file holding the exact same rows as plain text, so that trade-off no longer needs making the same way: paging is re-enabled across all five Complete Views (page size 1000), since a viewer who wants every row at once can open or download the TSV instead of forcing the browser to render the whole population into the DOM. The flat Leaderboard tables, already paged at 50, are unaffected.
