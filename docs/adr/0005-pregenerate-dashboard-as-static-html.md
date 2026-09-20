---
status: accepted
---

# Pregenerate the Dashboard as static HTML, not a live web app over SQLite

Viewers (the AMS team, executives, and clients) need to see usage reports, but the underlying aggregate data changes at most once a day, when the existing Daily Batch runs. We considered a small web app that queries the SQLite aggregate database on each page request, but rejected it: there is no fresher data to serve than the last Daily Batch produced, so a live query engine buys nothing while adding a server process that must be run, kept up, and secured.

Instead, a Regeneration Run — chained onto the existing daily import, per [ADR-0001](0001-sqlite-for-aggregate-store.md)'s single-writer batch design — rebuilds the whole Dashboard as static HTML. The output is a set of files that can be copied to any web server or file share and viewed with nothing more than a browser.
