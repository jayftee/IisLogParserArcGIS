Type: grilling
Status: resolved

## Question

What is the exact shape of the Included Root configuration that the Reports project reads to decide which Roots appear in the Dashboard?

The user has already compiled the list of Roots they want included. Resolve, with them:
- Does the config hold bare Root values only (matching the existing 16-char-truncated, lowercased normalization the parser already applies — see `RootNormalizer`), or does it also need a friendly display name per Root/site for client-facing pages (since a truncated lowercase Root like `titan` may not be the name clients or executives know it by)?
- Where does this config live physically — `appsettings.json` in the new Reports project (matching the existing parser's config pattern), or a separate file?
- What happens to a Root present in the aggregate database but absent from this list: silently excluded from every report (per the map's "Included Root" definition), or should the Global section still surface an "other/unlisted" rollup so nothing is invisible?
- Reconcile the user's already-compiled list against the actual normalized Root values the parser produces, to catch any mismatch before it silently excludes a Root the user meant to include.

## Note (not a resolution)

While resolving ticket 01, the user gave the real 14 Included Roots (all carrying ArcGIS Server traffic) while sketching the sidebar nav: ArcGIS, Charon, Deimos, Europa, Galatea, Iapetus, Kerberos, Mimas, Namaka, Oberon, Phobos, Rhea, Titan, Umbriel. This ticket's own questions (config shape, friendly display names, physical file location, unlisted-Root handling, reconciliation against `RootNormalizer`'s actual output) are still open — this just saves re-asking for the list itself when this ticket is picked up.

## Answer

**Reconciliation against `RootNormalizer`'s actual output** (checked by scanning the full `2026/` corpus — 720 files, ~257MB — through the same lowercase/truncate-to-16 logic): all 14 named Roots match exactly, no case or truncation mismatches. Two unlisted Roots turned up with real traffic:
- `portal` (147,812 hits — the single largest Root in the corpus): the Portal Web Adaptor itself.
- `proxy` (2,807 hits): a legitimate ArcGIS resource proxy (`/proxy/proxy.ashx`, `/proxy/oasis/proxy.ashx`, etc.) that forwards calls to the real underlying resource.

**Included Root config = the 14-name allow-list + the already-existing `PortalWebAdaptorName` setting, combined.** The new allow-list is a flat array of bare, lowercase ArcGIS Server site names (`arcgis`, `charon`, `deimos`, `europa`, `galatea`, `iapetus`, `kerberos`, `mimas`, `namaka`, `oberon`, `phobos`, `rhea`, `titan`, `umbriel`) and gates the ArcGIS Server section only. Portal is shown unconditionally — its rows in `aggregated_by_portal_item` were already correctly scoped at ingest time via `PortalWebAdaptorName`, and that table has no Root column to filter on regardless. The Reports project does not need its own copy of `PortalWebAdaptorName`; Portal Item rows need no Root filtering at all.

**`proxy` stays excluded**, not added as a 15th Included Root: since it forwards to the real underlying resource, which is logged and counted separately under its own real Root, including `proxy` would double-count those calls. It's silently excluded like any other unlisted Root — its traffic remains in the aggregate DB, queryable directly if ever needed.

**Config shape**: a flat array of bare, lowercase Root values — no separate friendly-display-name field. The names (ArcGIS, plus solar-system moons) are already what viewers recognize; no capitalization/display-name mapping is needed.

**Physical location**: `appsettings.json` in the new Reports project.

**Unlisted-Root handling**: silently excluded from every report, matching the map's existing "Included Root" definition — no "other/unlisted" rollup. Data remains queryable directly against the aggregate DB for fringe cases (e.g. "how many calls hit a specific proxy").
