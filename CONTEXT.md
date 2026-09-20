# IIS Log Parser for ArcGIS

Parses daily IIS W3C log files into replaceable per-day aggregate tables (by URI, root, user agent, referer, forwarded-for IP, referer+URI, ArcGIS Server service, Portal item, Field Maps device, and Survey123 device). Stores aggregates only — never raw log rows.

## Language

**UTC Date-Time**:
The log line's `date` + `time` fields combined, in UTC/GMT — the timestamp as IIS recorded it. Transient parsing output only: used to derive Local Date-Time, never persisted on an aggregate row.

**Local Date-Time**:
UTC Date-Time converted using the configured local time zone. Transient parsing output only: used to derive Local Date, never persisted on an aggregate row.

**Local Date**:
The date-only part of Local Date-Time. The sole temporal value persisted on aggregate rows, and the date component of every aggregate's grouping key.

**Hit**:
One log line that matched a given aggregate's dimension key. Counted once per line regardless of outcome — success/failure is only distinguished for the ArcGIS Server Service and Portal Item aggregates (see Successful Hit / Failed Hit).

**Daily Batch**:
The complete set of aggregate rows produced for one Local Date, built fully in memory from that day's log lines and written as one replaceable unit. Re-running a Local Date replaces its prior batch rather than merging with it — a Daily Batch is never incrementally updated. The one exception is the username back-fill (one per by-device aggregate: Field Maps and Survey123): right after a batch is written, in the same transaction, it may fill a still-empty `username` on rows of *other* Local Dates (see Field Maps Device, Survey123 Device). It never changes hits or time taken and never overwrites an existing username.

**Aggregate Row**:
One dimension-grouped record (one URI, one root, one ArcGIS service, etc.) for one Local Date, holding the accumulated `hits` and `time_taken_second` for every Hit that shares that key.

**Root / Site**:
The first non-empty path segment of a URI stem (e.g. `/mimas/rest/...` → `mimas`), lowercased and truncated to 16 characters. Called `root` in the by-root aggregate and `site` in the ArcGIS Server Service aggregate — the same concept under two column names, one per table.

**ArcGIS Server Service**:
Identified by the tuple (Root/Site, folder, service_name, service_type), parsed from a URI stem shaped like `/site/rest/services/[folder/]service_name/service_type/...`. `folder` is absent for folderless services. The trailing operation segment (`export`, `query`, `applyEdits`, …) is not part of the identity.
_Avoid_: "endpoint" for this concept — endpoint refers to the raw `uri_stem` used by the by-URI aggregate, a different dimension.

**Portal Item**:
Identified by `portal_item_id`, parsed from a URI stem shaped like `/<configured web adaptor name>/sharing/rest/content/items/<id>[/...]`, or its user-scoped (`.../content/users/<username>/items/<id>[/...]`) or user+folder-scoped (`.../content/users/<username>/<folderId>/items/<id>[/...]`) equivalents. The trailing sub-resource/operation segment, and any username/folder segments, are read structurally only to locate `items`/`<id>` and are not part of the identity — every request against the same id counts toward the same row regardless of which sub-resource, operation, username, or folder was involved. The configured web adaptor name is this deployment's own Portal Web Adaptor (`PortalWebAdaptorName`, default `portal`), not a fixed convention.
_Avoid_: "endpoint" for this concept, for the same reason as ArcGIS Server Service above.

**Successful Hit / Failed Hit**:
A Hit against the ArcGIS Server Service or Portal Item aggregate is a Successful Hit when `sc-status < 400`, otherwise a Failed Hit. Every such Hit is exactly one or the other; `hits = successful_hits + failed_hits` always holds.

**Field Maps Device**:
One install of the ArcGIS Field Maps mobile app, identified by `device_id` — the casefolded GUID in the trailing parenthesised group of a `cs(User-Agent)` that contains `arcgis-fieldmaps`. Not a physical handset: reinstalling the app produces a new Field Maps Device, and one user can have several. The Field Maps device aggregate holds one row per Field Maps Device per Local Date, with a `username` that is `NULL` until a Login Line for that device is matched — on the same Local Date, or back-filled from any other. There is no successful/failed split: the aggregate exists to show who is using Field Maps, not whether individual requests succeeded. A row whose `username` is still `NULL` is unattributed, which is expected for most devices whose last login predates the harvested history, so absence of a user is not evidence of non-use.
_Avoid_: "phone", "handset", "session"

**Survey123 Device**:
One install of the ArcGIS Survey123 app (the field app, or the Survey123 Connect desktop authoring tool), identified by `device_id` — the casefolded 32-hex id (no hyphens, as logged) that is the last `;+`-separated item of the first parenthesised group after `AppFramework/<version>+` in a `cs(User-Agent)` that contains both `AppFramework` and `Survey123`. Not a physical handset: reinstalling the app produces a new Survey123 Device, and one user can have several. The Survey123 device aggregate has the same shape and attribution rules as the Field Maps one (one row per Survey123 Device per Local Date, `username` `NULL` until a Login Line is matched on the same or any other Local Date, no successful/failed split). Survey123 traffic that carries no device id (the browser web app, identified only by its referer, and the `ArcGISRuntime-Qt` variant) is not part of it, nor is `AppFramework` traffic from another app such as QuickCapture. A Survey123 Device that was lent between users keeps one username (the earliest login date's, ties alphabetical) for its unattributed rows.
_Avoid_: "phone", "handset", "session"

**Login Line**:
A request from a Field Maps Device or Survey123 Device whose URI stem is `/<configured web adaptor name>/sharing/rest/community/users/<username>[/...]` and whose `sc-status < 400` — the only place a username appears, because the app requests it once when it starts and every later request from that device carries none. Used to attribute a device to a username (the HTTP method is not examined: Field Maps logs `GET`, Survey123 `POST`); the list of Login Lines is held in memory for the harvesting day only and is never persisted. The username is casefolded and comes from the URI stem, not from `cs-username`.
_Avoid_: "authentication", "sign-in record"

### Reporting

**Dashboard**:
The complete generated set of static HTML pages a viewer browses to see report data — the home page down through every section and Detail Page. Produced by a Regeneration Run and never assembled by hand.
_Avoid_: report site, web site — "site" is already taken by Root/Site and ArcGIS Server Service.

**Harvest Run**:
One execution that parses a single Local Date's IIS log lines and replaces that date's Daily Batch in the aggregate database. Never triggers a Regeneration Run itself — the Dashboard reflects a Harvest Run's data only once a separate Regeneration Run is performed.

**Regeneration Run**:
One execution that rebuilds the entire Dashboard from the aggregate database's current contents, replacing every previously generated page. Always a full rebuild — a Regeneration Run never updates the Dashboard incrementally — and stands on its own: it operates on whatever the aggregate database currently holds, whether or not a Harvest Run has just preceded it.

**Included Root**:
A Root/Site named in the Reports project's own configuration, and therefore represented in the Dashboard. A Root absent from this list — scanner or pen-test traffic hitting an unrecognized host, say — is still aggregated as usual but never appears in any report.
_Avoid_: allow-listed root, whitelisted root

**Detail Page**:
A Dashboard page scoped to a single ArcGIS Server Service, Portal Item, Field Maps Device, or Survey123 Device, showing that one entity's own request evolution over the year (for a Field Maps Device or Survey123 Device: its hits per day, headed by its username or "Unattributed" and its device id). Reached only from its section's Complete View, never from the sidebar.
_Avoid_: drill-down page, item page

**Leaderboard**:
A ranked top-N-by-hits view of one dimension (Portal Item, Field Maps Device, Survey123 Device, folder/service/type, forwarded-for IP, referer, URI, or user agent), rendered as a bar chart or table — either embedded in a section page or as its own standalone Dashboard page.
_Avoid_: top list, ranking page
