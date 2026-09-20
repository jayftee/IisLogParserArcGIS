# 20 — ArcGIS Field Maps usage attributed to a user (device-id ↔ username matching)

**What to build:** Aggregate ArcGIS Field Maps (the mobile app) traffic per **device**, then attribute each device's usage to a **username**. The device is identified by a GUID the app puts in its `cs(User-Agent)`, but the username is only visible on a small number of "login" requests made when the user starts the app — every later request from that device carries no user information. So the work is a match in two stages: (1) inside one Harvest Run, tally Field Maps usage per device, collect device → username from that day's login lines and join them on the normalized device id; (2) a **post-process** after the Daily Batch is written, which fills in the username on rows the day's own logins could not attribute, using logins found on *any other* date already in the database. Rows that still have no username are kept (username `NULL`), never dropped.

**Status:** done

One new aggregate table, `aggregated_by_field_maps_device`, in the same shape as the others (one row per key per Local Date). The authentication list used to find usernames exists only in RAM for the harvesting day and is never persisted; it is not a table. Per-user totals are a `SUM … GROUP BY username` query over this one table, not a second table.

## Purpose

The table exists to validate who is actually using their Field Maps licence and who is not — so only *usage by user* matters (hits and time taken); success vs. failure of individual requests is irrelevant, which is why there is no successful/failed split.

Consequence to keep in mind: a licensed user whose device never produces a matched login is indistinguishable from a non-user (their rows stay `NULL`, and they never appear against their username). In the 2026 sample that is 375 of 429 devices, mostly because their last login predates the sample window. So **absence from this table is not evidence of non-use** until enough history has been harvested. Because stage 2 is order-independent, harvesting older logs later attributes those devices retroactively, which is the fix — more history, not more code.

## The table

```sql
CREATE TABLE IF NOT EXISTS aggregated_by_field_maps_device (
    id INTEGER PRIMARY KEY,
    local_date TEXT NOT NULL,
    device_id TEXT NOT NULL,          -- casefolded GUID
    username TEXT NULL,               -- NULL until a login is matched or back-filled
    time_taken_second REAL NOT NULL,
    hits INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_field_maps_device_device_id ON aggregated_by_field_maps_device (device_id);
```

One row per (`local_date`, `device_id`). `time_taken_second` and `hits` follow the accumulation rules of the other aggregates. No `successful_hits`/`failed_hits` split — not requested; the Successful/Failed Hit definition in `CONTEXT.md` applies only to the ArcGIS Server Service and Portal Item aggregates. The device id has to be stored, because stage 2 can only back-fill a `NULL`-username row if the row still knows its device.

## Parsing rules

### 1. Field Maps request (usage)

A log line is a Field Maps request when its `cs(User-Agent)` field contains `arcgis-fieldmaps` (case-insensitive). The **device id** is the GUID in the last parenthesised group at the very end of the user-agent — the app appends it after the version:

```
… arcgis-fieldmaps/<version>+(<GUID>)      ← end of the UA field
```

`<GUID>` is 8-4-4-4-12 hex. Parsing works from the end of the string (see *The simplification* below); nothing before the trailing group is needed. A Field Maps line with no such GUID is not attributable to a device and is ignored (none exist in the 2026 sample corpus — 22,538 of 22,538 Field Maps lines carry one).

Every Field Maps request with a device id is a **Hit** for that device: `hits` counts lines, `time_taken_second` sums `time-taken` (converted from ms to seconds, same as every other aggregate). This includes the login lines themselves.

### 2. Login line (device → username)

Among Field Maps requests, a **login line** is one whose `cs-uri-stem` is either of:

```
/<W>/sharing/rest/community/users/<username>
/<W>/sharing/rest/community/users/<username>/<anything>
```

`<W>` is the configured Portal web adaptor name (`PortalWebAdaptorName`, default `portal`, from ticket 18) — do not hardcode `portal`. Matching follows the segment-based, case-insensitive, non-regex style of `PortalItemIdentityParser` / `ArcGisServiceIdentityParser`: split on `/`, check `<W>`, `sharing`, `rest`, `community`, `users` positionally, take the next segment as `<username>`, ignore whatever follows it. The username is not in `cs-username` (that column is `-` on these lines); it exists only in the stem.

Observed shapes: `user00364@somewhere`, `user00363@somewhere`, and email-style `user00353@somewhere`. No percent-encoding was observed, but the segment should be URL-decoded defensively before storing. Case: the real corpus contains `/Portal/sharing/...` (capital P) — the case-insensitive segment match is what catches it, a case-sensitive `/portal/` match silently misses these.

**Only successful logins should count.** The corpus has one `401` login line (`user00385@somewhere`, 2026-05-26): the username in the URL is client-supplied, so a rejected request must not bind a device to a user. Require `sc-status < 400`, consistent with the Successful Hit definition in `CONTEXT.md`.

## Device id normalization — `casefold`

Compare and group device ids after a **casefold** (`ToLowerInvariant()`, or `string.Equals(..., OrdinalIgnoreCase)` for comparison only), and store the normalized form. Reason: the GUID's case is decided by the client platform, not by the device, so the raw string cannot be trusted as a stable key.

| Runtime prefix (start of UA) | Platform | GUID case observed | Lines (2026 corpus) |
|---|---|---|---|
| `ArcGISMaps-Swift` | iPhone / iPad | UPPER | 18,171 |
| `ArcGISRuntime-iOS` | iPhone / iPad (older Runtime) | UPPER | 1,489 |
| `ArcGISMaps-Kotlin` | Android | lower | 2,789 |
| `ArcGISRuntime-Android` | Android (older Runtime) | lower | 89 |

No device in the sample changes case between requests, so today an un-normalized match would happen to work. Normalizing is still the right rule: it makes the join independent of which runtime produced a given line and it prevents a silent drop the day a device is seen under two runtimes (e.g. before/after an app update).

## iPhone vs Android — what actually differs

The GUID **position is the same** on both platforms: `arcgis-fieldmaps/<version>+(<GUID>)`. What differs, and what a parser must survive:

- **Case** of the GUID (above).
- **Runtime token**: `ArcGISMaps-Swift` / `ArcGISRuntime-iOS` vs `ArcGISMaps-Kotlin` / `ArcGISRuntime-Android`. The Swift runtime version is sometimes two-part (`200.8`) and sometimes three (`200.8.1`).
- **The first parenthesised group is the platform/device group**, not the GUID: `(iOS+26.2;+iPhone15,5)` vs `(Android+16.0;+arm64-v8a;+SAMSUNG-SM-S938W)`. It varies by device model (186 distinct platform groups in the sample alone, and the sample is only a subset of the real device population), so it must not be parsed or depended on.
- **Android model names can themselves contain parentheses**, e.g. `MOTOROLA-MOTO-G-POWER-(2022)` (36 lines in the sample), so counting or matching `(` from the *front* of the UA is unreliable.
- **`ArcGISRuntime-Android` writes a doubled plus** before the token (`…+SAMSUNG-SM-A536W)++arcgis-fieldmaps/…`) where every other runtime writes one. Don't depend on the characters before `arcgis-fieldmaps`.
- iOS also reports iPad (`iPad13,4`, `iPadOS`), which is still the same shape.

### The simplification: the device id is the trailing parenthesised group

Validated against every Field Maps user-agent in the 2026 corpus (22,538 of 22,538): the user-agent **ends** with the device GUID wrapped in parentheses, appended after `arcgis-fieldmaps/<version>+`, with nothing after it. Every UA has exactly one GUID-shaped group, and it is always the last one; the platform group and any parenthesised model-name fragment always come *earlier*. So the parser can work **from the end of the string** and never look at the runtime token, platform group, model name or `++`:

1. The UA (already known to contain `arcgis-fieldmaps`) must end with `)`.
2. Find the last `(`; the text between it and the final `)` is the candidate id.
3. Accept it only if it is a well-formed 8-4-4-4-12 hex GUID (e.g. `Guid.TryParseExact(..., "D")`); otherwise no device id.

Step 3 is what keeps this safe: taking the last `(...)` alone would silently return garbage for a UA that ends in some other parenthesised group, so the GUID-shape check is required, not optional. A UA with the token but no valid trailing GUID yields no device id (none exist today). No regex needed, in keeping with the other parsers here. This supersedes any need to anchor on the version/`+(`: the tail rule is strictly simpler and independent of runtime naming.

## Task, stage 1: aggregate in RAM, match logins, then commit

While parsing a day's lines, keep two things in memory:

1. **The aggregated rows** — keyed by normalized device id: running `hits` and `time_taken_second` for every Field Maps request (including login lines). These become the table rows.
2. **The login list** (RAM only, this day only) — keyed by normalized device id: the username seen on a successful login line that day. If a device is seen with several usernames that day, the earliest successful login by timestamp wins and later ones are ignored (first-write-wins over timestamp-ordered lines, e.g. `TryAdd`) — no special handling, no warning.

When the day's lines are exhausted, walk the aggregated rows, look each device id up in the login list, and populate `username` on every row that has a match. **A row with no same-day login is kept with `username = NULL`** — it is not dropped. Then the Daily Batch is committed (stage 1 ends), and stage 2 runs. Raw log lines and the login list are, as always, never persisted.

This is a Domain-layer aggregator (`Domain/Aggregation/`), following the same pure-function shape as the existing ones (`Aggregate(requests, localDate, portalWebAdaptorName)` style), plus a parser for the Field Maps GUID and one for the login stem (mirroring `PortalItemIdentityParser`).

## Task, stage 2: post-process — back-fill usernames from other days

Motivation: the app only reveals the username when it starts, so a device's usage on days without a login line has no user. Stage 1 alone leaves those rows `NULL` even though the same device was identified on another date. Stage 2 fixes that with data already in the database.

Runs **after** the Daily Batch has been inserted (same connection, ideally inside the same transaction as `DailyAggregateReplacer.Replace` so a failure rolls both back — but it is idempotent and self-healing, so a separate step is acceptable):

1. Select the distinct device ids of Field Maps rows where `username IS NULL` (across **all** local dates, not only the date just harvested).
2. For each such device, select **one** username from that device's rows where `username IS NOT NULL` (again across all dates): `SELECT username … WHERE device_id = @device AND username IS NOT NULL ORDER BY local_date, username LIMIT 1`. The `LIMIT 1` is the point — the code never sees, and never has to handle, a device with several usernames; whichever the query returns first is used.
3. If a username came back, `UPDATE` every `NULL`-username row of that device to it. If none, leave the rows `NULL`.

The `ORDER BY` is required, not decoration: `LIMIT 1` without it returns an arbitrary row, which would make the result differ between runs and break the idempotence below. `local_date, username` means "the earliest login date's user, ties broken alphabetically".

The per-device loop is the described behaviour; the same result is a single set-based `UPDATE`, which is what shipped (see Comments for the form used and why), and it fits the Dapper repository split (ADR 0002). Either way, index the device-id column.

Properties this gives us, all worth a test:

- **Idempotent and re-runnable.** Running it twice changes nothing the second time; re-harvesting a date replaces that date's rows (username re-seeded from stage 1) and stage 2 re-fills them.
- **Order-independent.** Harvest days in any order (backfill, ticket 17): a login found on 20 Jan retroactively attributes the `NULL` rows on 10 Jan and 30 Jan the moment 20 Jan is harvested. Likewise, backfilling an older year later attributes devices that only logged in then.
- **Only ever fills `NULL`s.** It never overwrites an existing username, so it cannot change what stage 1 established.
- **Touches other dates' rows.** This is a deliberate exception to "a Daily Batch is written as one replaceable unit" (see `CONTEXT.md`): the batch is still replaced whole, but stage 2 then updates `username` on rows of *other* dates. The glossary entry for **Daily Batch** should note this.

Assumption to record: a device belongs to one user over time, so a login on one date is valid for the device's `NULL` rows on earlier and later dates. The corpus supports this today (every one of the 54 logged-in devices has exactly one username) and where it doesn't hold (a lent or shared device) the effect is small and accepted — see *Open questions* 3.

Direction matters here: **one user with many devices is expected and needs no special handling** (14 of 29 users have several; each device still resolves to exactly one username, so a user who replaces or adds devices during the year simply appears against each of them). The reverse — **one device with more than one distinct username** (a lent or shared device) — is the only ambiguous case, and it is resolved by `LIMIT 1` rather than by code.

## Data from the 2026 sample corpus (why the open questions matter)

- 22,538 Field Maps lines, all with a GUID; **429 distinct devices** after casefold; no device seen under two cases.
- 130 login lines (all `GET`; 129 × `200`, 1 × `401`) from **54 devices** and **29 distinct users**.
- Each of those 54 devices maps to exactly one username. 14 of the 29 users have more than one device (phone + tablet, or device replaced).
- **375 of 429 devices (17,701 of 22,538 hits, ≈79 %) never have a login line anywhere in the corpus** and will remain `NULL` after stage 2. Most of that is real: those devices' users were already logged in before the sample window began. They only get a username if a later (or earlier-year) harvest turns up their login.
- **Login and usage are often on different days.** Counting (device, day) pairs of Field Maps usage: 57 have a login the same day (3,916 hits); **148 have a login only on some *other* day (921 hits)**, with the nearest login a median of 24 days away (p90 85, max 107); 1,218 never log in at all (17,701 hits, the unattributed share above). A Harvest Run builds one Local Date's Daily Batch in memory from that day's lines only, so stage 1 alone leaves those 921 hits `NULL` even though the device is known — stage 2 exists to recover them. With it, 4,837 hits (21.5 %) are attributed (3,916 same-day + 921 cross-day) and 17,701 remain `NULL`.

## Open questions

1. ~~Cross-day device → username lookup~~ — **resolved:** keep unmatched rows with `username = NULL` and run the stage-2 post-process (this ticket, as amended). No separate lookup table, no log look-back.
2. ~~Table shapes~~ — **resolved:** one table, `aggregated_by_field_maps_device` (above). The login list is RAM-only. Per-user figures are derived by query, not stored. The device id is persisted because stage 2 needs it as the join key.
3. ~~Device with several usernames~~ — **resolved:** not handled in code. A device lent to someone else is unlikely, and a login one day followed by the borrower using it through the night into the next day is rarer still, so the tallying error is negligible. Stage 1 keeps the earliest login (by timestamp) for the day; stage 2's query returns a single row via `ORDER BY local_date, username LIMIT 1`. No warning, no conflict path, no test for "conflict" behaviour beyond asserting that exactly one username is used.
4. **Username casing (default chosen).** Usernames appear lowercase in the sample but Portal usernames are case-insensitive. Casefold the stored username too, for the same fragmentation reason tickets 07/12 lowercased root and service names. Say so if that is not wanted.
5. **Run summary (default chosen).** Report, per Harvest Run, devices seen, attributed by same-day login, attributed by stage 2, and still `NULL`, so the ≈79 % unattributed share is visible rather than silent.
6. **Reports (out of scope, follow-up).** No Dashboard page is part of this ticket. When one is built over this table it must decide how to show `NULL`-username rows (exclude, or an explicit "Unattributed" bucket) — otherwise per-user totals silently understate Field Maps usage by ≈79 %.

## Acceptance criteria

- [x] `FieldMapsDeviceIdParser` extracts and casefolds the device GUID from the trailing parenthesised group of a user-agent; covers iOS Swift (2- and 3-part runtime version), `ArcGISRuntime-iOS`, Android Kotlin, `ArcGISRuntime-Android` with the doubled `+`, an Android model containing parentheses (`MOTOROLA-MOTO-G-POWER-(2022)`), iPad, a non-Field-Maps UA (no match), a Field Maps UA without a trailing group (no match), a Field Maps UA whose trailing group is not a GUID (no match — proves the shape check is what guards the last-group rule), a malformed/short GUID (no match), and a UA with trailing characters after the `)` (no match).
- [x] `FieldMapsLoginParser` extracts the username from `/<W>/sharing/rest/community/users/<username>[/...]`; honors a non-default `PortalWebAdaptorName`, is case-insensitive on every structural segment (including `/Portal/`), handles email-style usernames, and does not match `community/users` (no username), `community/groups/…`, or `community/users?q=…`-style search stems.
- [x] Only `sc-status < 400` login lines register a device → username binding.
- [x] Usage tally keys on the casefolded device id; the same device seen as `…AAAA…` and `…aaaa…` is one entry.
- [x] Stage 1: usage entries with a same-day login are emitted with the username; usage entries without one are still emitted with `username = NULL` (not dropped). The casefolded device id is persisted on the row, and the login list itself is never persisted.
- [x] Stage 2 post-process: after the Daily Batch insert, `NULL`-username rows of any date are back-filled from the first username (`ORDER BY local_date, username LIMIT 1`) found for the same device on any date; leaves rows `NULL` when no username exists; when a device has several usernames, uses that first one deterministically and neither fails nor warns; never overwrites a non-`NULL` username; idempotent on a second run; order-independent (harvesting a login day after the usage day yields the same final state as the reverse order); re-harvesting a date yields the same final state.
- [x] `Data.Tests` cover the stage-2 update: cross-date fill, no-match untouched, multi-user device gets exactly one (the earliest-date) username, non-`NULL` never overwritten, other devices' rows unaffected.
- [x] `Domain.Tests` cover the stage-1 aggregator: match, unmatched-kept-as-`NULL`, date filtering, hits/time-taken summing, case-only-different device ids merged, login line itself counted as a hit, null-argument guards.
- [x] `Data.Tests`, schema, `AggregateTableNames.All`, `DailyAggregateBatch`, `DailyAggregateReplacer`, `Program.cs` and `README.md`/`CONTEXT.md` (new glossary terms: **Field Maps Device**, **Login Line**) updated for the one new table (ticket 18 is the closest wiring pattern), including the schema/index above.
- [x] Spot-check against the real 2026 corpus for at least one day with a same-day login and one with a cross-day login, and record the same-day / stage-2 / still-`NULL` device and hit counts in the ticket comments (expected from the analysis above: 4,837 hits attributed, 17,701 `NULL`, across the full year).

## Comments

Implemented in `df0f3e3`, in two stages plus the shared wiring, test-first at the seams named below.

**Domain** (`src/IisLogParserArcGIS.Domain/Aggregation/`): `FieldMapsDeviceIdParser` reads the device id from the *end* of the user-agent (must contain `arcgis-fieldmaps`, end in `)`, and the last parenthesised group must be a well-formed GUID via `Guid.TryParseExact(..., "D")`), returning it casefolded. `FieldMapsLoginParser` is segment-based and case-insensitive in the style of `PortalItemIdentityParser` (honors the configured `PortalWebAdaptorName`, matches `/Portal/...`), URL-decodes and casefolds the username. `ByFieldMapsDeviceAggregator` tallies every Field Maps line per device (login lines included), collects a RAM-only login list of successful (`sc-status < 400`) logins and stamps each row's username from it; a device with no login that day is kept with a `NULL` username. `ByFieldMapsDeviceAggregateRow` and `FieldMapsAttributionCounts` (devices seen / same-day / back-filled / unattributed) round it out.

**Data**: new table `aggregated_by_field_maps_device` (nullable `username`, index on `device_id`) added to `AggregateTableNames.All`, `AggregateDatabaseSchema`, `DailyAggregateBatch` and `DailyAggregateReplacer` (nine tables now). `ByFieldMapsDeviceRepository.BackfillUsernames` is a single set-based `UPDATE` run by `DailyAggregateReplacer` *inside the replace transaction, after the inserts*, so a failure rolls both back. It never overwrites a username and returns the number of rows it filled (0 on a second run).

**Deviation from the ticket text, on purpose:** the first cut used the ticket's correlated subquery (`... ORDER BY local_date, username LIMIT 1` per empty row). `/code-review` flagged the whole-table scan, and a benchmark at production-like scale confirmed it was a real problem (about 11.7 s per harvest at 650k rows, 25.7 s at 1.3M). The shipped form computes each device's first username once in a `MATERIALIZED` CTE (`ROW_NUMBER() OVER (PARTITION BY device_id ORDER BY local_date, username)`) and joins it back: about 0.8 s and 3.1 s for the same tables, and identical output on a 270k-row table that included multi-username devices and same-date ties. The ordering rule (earliest `local_date`, ties alphabetical) is unchanged.

**Also from review:** stage 1 now takes the earliest login *by timestamp* rather than the first in file order, so re-running a date cannot change the winner if lines arrive out of order.

**Host**: `Program.cs` aggregates the new rows and logs a `Field Maps attribution:` line per harvest via `RunSummaryReporter.ReportFieldMapsAttribution` (devices seen, same-day, back-filled, still unattributed) at the same Warning level as the existing run summary. The "back-filled" figure counts what that harvest filled at the time, so it depends on harvest order: rows filled *later* by another date's harvest are not reported by this one.

**Docs**: `README.md` (eight → nine throughout, ER diagram and table, class diagrams, new "Domain: Field Maps username matching" section with a flowchart, replacer paragraph), `CONTEXT.md` (new **Field Maps Device** and **Login Line** terms; **Daily Batch** now notes the back-fill exception), `docs/adr/0002` (eight → nine).

**Real-corpus spot-check** (the checked-in 2026 corpus, `America/Edmonton`, real CLI `harvest` once per local date, 2026-01-02 through 2026-09-09 = 251 dates, into a scratch database):
- Forward date order and reverse date order produced **identical databases** (1,228 rows each), and re-running forward after the SQL change reproduced them exactly, so stage 2 is order-independent and idempotent on real data.
- 22,497 Field Maps hits across 429 devices (the 41 fewer than the 22,538 in the earlier standalone analysis fall on the two boundary local dates not harvested). **4,837 hits (21.5 %) attributed; 17,660 `NULL`**, matching the analysis's predicted 4,837 attributed. 54 devices attributed, 29 distinct usernames, no device with more than one username, no upper-case ids or usernames, and no `NULL` row whose device has a username on any other date.
- Summed per-harvest summary lines, forward order: 1,228 device-days = 56 attributed same-day + 53 back-filled at harvest time + 1,119 unattributed (in reverse order the split is 56 / 121 / 1,051 because more donors already exist when earlier dates are harvested; the final state is the same).

**Tests**: `Domain.Tests` 215 (51 new: device-id parser 17, login parser 18, aggregator 13, counts 3), `Data.Tests` 134 (13 new: repository round trip and delete scoping, nine back-fill properties, schema nullable-username and index, replacer back-fill in both harvest orders and nine-table assertions), host `Tests` 58 (2 new). All pass, solution builds with 0 warnings. Not caused by this ticket and unchanged from a clean `main`: `RegressionTests` (4 failing, the line-count oracle does not expect two site-id files per date) and `Reports.Tests` (68 failing / 43 passing, the fixture cannot harvest local date 2026-09-10 for lack of a 2026-09-11 UTC file — already documented in reports ticket 19).

**Known residual risk, accepted:** any `community/users/<name>` request from a Field Maps device is treated as that device's login, so if the app ever looks up *another* user's profile before the signed-in user's own, that name would be kept for the day. The 2026 corpus shows no such case (each of the 54 logged-in devices has exactly one username), so no filtering was added; if it ever appears the device-to-username pairing would need an extra signal.
