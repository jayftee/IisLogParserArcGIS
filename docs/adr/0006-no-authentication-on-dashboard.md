---
status: accepted
---

# No authentication or access control on the Dashboard

Dashboard viewers include the AMS team, executives, and clients who want to see their own service's usage. We considered scoping each client to only their own Root/Site, which would require some notion of per-viewer permissions and filtered output.

We rejected that: the Dashboard shows operational usage metrics — hit counts, response times — not sensitive per-client content, and the added complexity of an access-control system isn't justified for this audience. The Dashboard has no login and no per-viewer filtering; every viewer who can reach it sees every Included Root's data. Getting a specific report in front of a specific client, if that's ever needed, is a distribution decision made outside the Dashboard (e.g. sending them a direct link), not something the Dashboard itself enforces.
