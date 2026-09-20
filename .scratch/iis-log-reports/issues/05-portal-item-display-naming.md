Type: grilling
Status: resolved

## Question

On Portal Leaderboards, the Portal item table, and each Portal Item's Detail Page, should the Portal Item be shown only by its raw `portal_item_id` (the only thing the aggregate table stores), or should the Dashboard resolve a friendly title for it?

The aggregate schema has no item title, only the 32-hex id. Resolving a title would mean either the Reports project calling the live Portal REST API (`.../sharing/rest/content/items/<id>`) during each Regeneration Run — which requires Portal reachability/credentials from wherever the Regeneration Run executes, and adds a per-item network call at generation time — or accepting raw ids in the Dashboard. Decide which, and if title resolution is wanted, whether missing/inaccessible items should just fall back to the raw id rather than fail the run.

## Answer

**No title resolution.** A Portal Item's title can change at any time at the owner's discretion, so any resolved title would need re-fetching every Regeneration Run anyway to stay accurate — there's no stable value worth caching, and it doesn't remove the per-run network dependency it was meant to avoid. The Dashboard shows the raw `portal_item_id` everywhere (Leaderboards, the Portal item table, Detail Pages).

**The raw id becomes a clickable link** to the item's live Portal page (`https://<portal-host>/home/item.html?id=<id>`), so a viewer can jump to Portal and see the current title/thumbnail themselves — no network call or credentials needed at generation time, just URL templating. This requires one new setting, since nothing in the solution currently captures a Portal base hostname (`PortalWebAdaptorName` is only a URI path segment): add `PortalBaseUrl` to the Reports project's own `appsettings.json`.
