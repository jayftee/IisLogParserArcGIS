# 21 — ArcGIS Survey123 usage attributed to a user (device-id ↔ username matching)

**What to build:** Aggregate ArcGIS Survey123 (the mobile/desktop app) traffic per **device**, then attribute each device's usage to a **username** — exactly the two-stage scheme ticket 20 built for Field Maps (stage 1: tally per device inside the Harvest Run and stamp the username from that day's in-memory login list; stage 2: a post-process that back-fills still-`NULL` usernames from any other date). **Only the detection rule for the user-agent differs**: what makes a line a Survey123 request, and where the device id sits inside the user-agent. Everything after that — the login-line stem, the `sc-status < 400` rule, casefolding, `NULL`-username rows kept, the back-fill's `ORDER BY local_date, username LIMIT 1`, the run-summary line, the table shape — is ticket 20 again, for a second app.

**Blocked by:** none — builds on `iis-log-parser` ticket 20 (done).

**Status:** done

Read ticket 20 first (`20-field-maps-usage-by-user.md`), especially *Task, stage 1*, *Task, stage 2* and its Comments (the shipped set-based `MATERIALIZED` back-fill, and why). This ticket only spells out what is different.

## Purpose

Same as ticket 20: validate who is actually using their Survey123 licence and who is not, so only *usage by user* matters (hits and time taken). No successful/failed split. Absence from the table is not evidence of non-use until enough history has been harvested; harvesting more history attributes devices retroactively (stage 2 is order-independent).

## What is different from Field Maps

### 1. Survey123 request (usage) — the user-agent must contain BOTH `AppFramework` AND `Survey123`

Case-insensitive substring match on `cs(User-Agent)`. **Both** tokens are required, and this is not decoration — each one alone matches unrelated traffic in the 2026 corpus:

| UA contains | Lines | What it is | Device id? |
|---|---|---|---|
| `AppFramework` **and** `Survey123` | **5,879** | the Survey123 app | yes |
| `AppFramework` only | 62 | ArcGIS QuickCapture (`…+QuickCapture/<v>+…`) | yes, but a different app — **must not** be counted |
| `Survey123` only (in the UA field) | 372 | `ArcGISRuntime-Qt/<v>+(iOS+…;+arm64;+Qt+…;+Survey123+<v>)` — a Runtime-embedded variant | **no** — no device id to key on; ignored |

Also do **not** match on the word anywhere else in the line: 561 more lines mention `survey123` only in the **referer** (`https://survey123.arcgis.com/` — the browser-based Survey123 web app, which carries no device id), and another 724 only in the `cs-uri-stem` (Survey123-published service names). Match the user-agent field only.

The product token has two forms in the corpus: `Survey123/<v>` (the field app: 5,785 lines) and `Survey123+Connect/<v>` (the desktop authoring tool, Survey123 Connect: 94 lines, 7 devices). The stated rule ("both AppFramework and Survey123 present") includes Connect, and this ticket keeps it that way — see *Decisions to confirm* 3.

### 2. The device id — a 32-hex string in the FIRST parenthesised group, not a trailing GUID

Field Maps ends its user-agent with `(<hyphenated GUID>)`. Survey123 does **not**: the device id is the **last `;+`-separated item of the first parenthesised group** (the one straight after `AppFramework/<v>+`), and it is 32 hex digits with **no hyphens** (a "GUID" in `N` format). The rest of the string (`Survey123/<v>+(Qt …)+<os>/<v>`) follows it, so the *tail rule from ticket 20 cannot be reused*.

Real shapes (masked; 5,879 of 5,879 lines match this structure, each with exactly one 32-hex string):

```
AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+<HEX32>)+Survey123/3.24.21+(Qt+5.15.6;+arm64-little_endian-lp64;+iOS+26.1)+darwin/25.1.0            4,762 lines, 154 devices
AppFramework/…+(Windows+10;+en_CA;+x86_64;+<HEX32>)+Survey123/…+(Qt+…;+x86_64-little_endian-llp64;+Windows+10+Version+2009)+winnt/…             ~720 lines, 47 devices
AppFramework/…+(Android+16;+en_CA;+arm64;+<HEX32>)+Survey123/…+(Qt+…;+arm64-little_endian-lp64;+Android++(16))+linux/<kernel-string>            ~400 lines, 20 devices
AppFramework/…+(Windows+10;+en_US;+x86_64;+<HEX32>)+Survey123+Connect/…+(Qt+…)+winnt/…                                                           the Connect variant
```

Parsing rule (structural, no regex, in the style of the other parsers here):

1. The UA must contain `AppFramework/` and `Survey123` (case-insensitive).
2. From the text after `AppFramework/`, find the first `(`, then the first `)` after it. That is the platform group.
3. Split the group on `;+`. The **last** item is the candidate device id.
4. Accept it only if it is exactly 32 hex digits (`Guid.TryParseExact(..., "N")` does exactly this). Otherwise there is no device id (none exist today).
5. Store it **as it appears, casefolded** — i.e. lowercase 32-hex with no hyphens (do **not** convert to the hyphenated `D` form: the logs and Survey123 itself use the `N` form, so the stored id stays greppable against the raw logs).

Anchoring on the first group is safe in the corpus: the platform group never contains nested parentheses (the `(…)` after `Android++` is in the *second* group, after the id). Platform-varying parts are the OS name/version, locale (`en_CA`/`en_US`), architecture, and the Qt/kernel strings — none of them are examined.

Casing: all 5,879 ids are lowercase today (no device seen in two cases, none on two platforms). Casefold anyway, for the same reason as ticket 20 (the case is decided by the client platform, so the raw string is not a trustworthy key).

### 3. Login line — same stem as Field Maps, but the method is `POST`

`cs-uri-stem` `/<W>/sharing/rest/community/users/<username>[/...]`, `<W>` = configured `PortalWebAdaptorName`, segment-based and case-insensitive (`/Portal/…` too). The shape is **identical to Field Maps' login stem**, so the same parsing rule applies (ticket 20's `FieldMapsLoginParser` already implements it and is not Field-Maps-specific in any way; see *Implementation guidance*). Differences observed:

- Survey123's login lines are **`POST`** (291 × `POST` / `200` in the corpus); Field Maps' were `GET`. Do not filter on method — the rule is: the device's own request, stem matches, `sc-status < 400`.
- All 291 are exactly `…/community/users/<username>` with no trailing sub-resource.
- Usernames are email-style and mixed-case: 121 of 291 login lines contain an uppercase character (`user00129@SOMEWHERE`, `User00305@SOMEWHERE`), so **casefold the username** (as ticket 20 does). No percent-encoding was observed; URL-decode defensively as before.
- `cs-username` is `-` on every Survey123 line, as with Field Maps: the username exists only in the stem.

## The table

One new aggregate table, same shape as ticket 20's, one row per (`local_date`, `device_id`):

```sql
CREATE TABLE IF NOT EXISTS aggregated_by_survey123_device (
    id INTEGER PRIMARY KEY,
    local_date TEXT NOT NULL,
    device_id TEXT NOT NULL,          -- casefolded 32-hex, no hyphens
    username TEXT NULL,               -- NULL until a login is matched or back-filled
    time_taken_second REAL NOT NULL,
    hits INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_survey123_device_device_id ON aggregated_by_survey123_device (device_id);
```

A separate table rather than an `app` column on the Field Maps table, following this project's one-aggregate-per-table convention (each table's `local_date` replace stays independent) and because a Field Maps device id and a Survey123 device id are different identifier spaces (hyphenated GUID vs 32-hex). Add it to `AggregateTableNames` (`All` becomes ten), `AggregateDatabaseSchema`, `DailyAggregateBatch` and `DailyAggregateReplacer`, wire the aggregation and back-fill into `Program.cs`, and update `README.md`/`CONTEXT.md`/`docs/adr/0002` (nine → ten), the wiring pattern of ticket 20.

Every Survey123 request with a device id is a **Hit**, login lines included; `time_taken_second` sums `time-taken` ms → s, as everywhere else.

## Stage 1 and stage 2

Exactly ticket 20: (1) in-memory login list for the harvesting day, earliest successful login by timestamp wins, unmatched devices kept with `username = NULL`, never dropped; (2) after the Daily Batch insert and inside the same transaction, back-fill every `NULL`-username row of any date from the device's first username (`ORDER BY local_date, username LIMIT 1` semantics, computed once per device with the `MATERIALIZED` CTE that ticket 20's Comments explain), never overwriting a username, idempotent, order-independent. Log a `Survey123 attribution:` line per harvest at the same level and in the same shape as `Field Maps attribution:` (devices seen, same-day, back-filled, still unattributed).

## Data from the 2026 sample corpus (why the numbers below are the acceptance targets)

Computed directly from the 720 checked-in `2026/` log files, by **UTC log-file date** (a Harvest Run buckets by *local* date, so the harvested split will differ slightly at the day boundaries — the same caveat as ticket 20's analysis):

- **5,879** Survey123 lines, **221 distinct devices** (154 iOS, 47 Windows, 20 Android; no device on two platforms, none in two cases, exactly one id per user-agent), 535 (device, day) pairs.
- **291** successful login lines, from **135 devices**, **124 distinct users**.
- Usage vs login, by (device, day): **291** device-days have a same-day login (2,246 hits); **69** have a login only on another day (1,020 hits); **175** never have a login (2,613 hits). With stage 2, **3,266 hits (≈55.6 %) are attributed** and **2,613 remain `NULL`** — a much better attributed share than Field Maps' ≈21.5 %, because Survey123 signs in far more often (291 login lines for 221 devices, vs 130 for 429).
- **4 devices have more than one distinct username** (`544ab5b4…` hailie/jailynn, `6efac5ac…` cathy/garrett, `e188d474…` courtenay/heather, `23bb75e5…` josh/katie), months apart — i.e. genuinely lent or shared devices. Field Maps had none. See *Decisions to confirm* 4.
- Zero overlap with Field Maps traffic (no UA carries both `arcgis-fieldmaps` and `Survey123`).
- Volume by month is bursty (5/2026 has 1,941 lines, 4/2026 has 248) — irrelevant to the design, noted so a spot-check date is picked where there is data.

## Decisions to confirm (defaults chosen; say if any is unwanted)

1. **Separate table** `aggregated_by_survey123_device` (above), not an extra column on the Field Maps table.
2. **Device id stored as logged** — lowercase 32-hex, no hyphens — not re-formatted into a hyphenated GUID.
3. **Survey123 Connect (desktop) is included**, as the stated "both tokens present" rule implies (94 lines, 7 devices). Excluding it would be a one-line change (`Survey123+Connect` token check) but mixes a design tool's usage out of a *licence-usage* report; flagged because a Connect user is arguably a licensed Survey123 user and arguably not a "field" user.
4. **Lent devices are not special-cased**, per ticket 20's open question 3: one username per device (earliest login date, ties alphabetical), so later usage by the second borrower is attributed to the first user. The effect is larger here than for Field Maps (4 of 135 logged-in devices, ≈3 %, vs 0 of 54), but still small; accepted the same way. If it turns out to matter, the fix is a per-(device, day) attribution, a separate ticket.
5. **User-agent only** — the referer `survey123.arcgis.com` browser traffic (no device id) is out of scope.

## Implementation guidance (not prescriptive)

- The **login parser is app-agnostic**: `FieldMapsLoginParser` already matches `…/community/users/<username>` and knows nothing about Field Maps. Reuse it (rename it to something app-neutral if you can do so cleanly) rather than copying it. Same for the aggregator's login-list logic and `ByFieldMapsDeviceRepository.BackfillUsernames`: prefer parameterizing over the table/parser rather than duplicating ~150 lines per app. If you choose to extract shared code, keep Field Maps' behaviour and tests unchanged.
- A new `Survey123DeviceIdParser` (mirroring `FieldMapsDeviceIdParser` in shape and test style) is the one genuinely new parser.
- `Survey123` in an identifier will trip nothing special, but mind `CC0309`-style naming analyzers that already object to "field" in `FieldMaps…` names, and the 3-argument `CC0042` limit — follow ticket 20's suppression style where a justified suppression is needed.

## Acceptance criteria

- [x] `Survey123DeviceIdParser` returns the casefolded 32-hex device id for a Survey123 user-agent and nothing otherwise. Covers: iOS, Windows, Android (kernel-string tail), the `Survey123+Connect` variant, `en_CA` and `en_US` locales, an uppercase 32-hex id (casefolded), `AppFramework` **without** `Survey123` (QuickCapture UA — no match), `Survey123` **without** `AppFramework` (`ArcGISRuntime-Qt/…Survey123+…` — no match), a Field Maps UA (no match), a Survey123 UA whose platform group's last item is not 32-hex (no match), a 31- and a 33-digit id (no match), a hyphenated GUID (no match), no parenthesised group after `AppFramework/` (no match), and a UA where the id is not the last `;+` item.
- [x] The login stem parsing used for Survey123 is covered for the Survey123 corpus shapes: `/portal/sharing/rest/community/users/<user>`, a mixed-case `/Portal/…`, an email-style mixed-case username such as `User00305@SOMEWHERE` (casefolded), a non-default `PortalWebAdaptorName`, and the non-matching stems (`community/users` with no username, `community/groups/…`, `portals/self`, `content/items/…`).
- [x] Only `sc-status < 400` login lines register a device → username binding; a `POST` login is accepted (method is not filtered).
- [x] Usage tally keys on the casefolded device id; hits and time-taken sum as in ticket 20; login lines count as hits; requests for other local dates are ignored; a device with no same-day login is kept with `username = NULL`.
- [x] Stage 2 back-fill for the Survey123 table has the same tested properties as ticket 20's: cross-date fill, no-match untouched, multi-username device gets exactly one (earliest date, then alphabetical), never overwrites a non-`NULL` username, idempotent, order-independent, other devices unaffected.
- [x] `aggregated_by_survey123_device` and its `device_id` index exist in the schema, in `AggregateTableNames.All` (ten), `DailyAggregateBatch`, `DailyAggregateReplacer` (deleted and re-inserted per Local Date, back-fill inside the same transaction), and `Program.cs`; a `Survey123 attribution:` line is logged per harvest.
- [x] Field Maps behaviour is unchanged: ticket 20's tests still pass untouched (or are only mechanically renamed if shared code is extracted), and no Field Maps hit is counted as Survey123 or vice versa.
- [x] `README.md` (nine → ten tables, ER diagram, table list, a Survey123 counterpart of "Domain: Field Maps username matching" or a note in it covering the different device-id rule) and `CONTEXT.md` (new term **Survey123 Device**, avoiding "phone"/"handset"; **Daily Batch** back-fill exception now covers both tables; **Login Line** no longer described as Field Maps-only) and `docs/adr/0002` updated.
- [x] Real-corpus spot-check, run the way ticket 20's was (real CLI, `harvest` once per local date, forward **and** reverse order into scratch databases): record same-day / back-filled / still-`NULL` device and hit counts in the ticket comments. Expected from the analysis above, across the full year and allowing for local-vs-UTC date bucketing at the boundaries: **≈5,879 hits, 221 devices, ≈3,266 hits (≈55.6 %) attributed, ≈2,613 `NULL`, 4 devices with more than one username**, forward and reverse order producing identical databases, and no `NULL` row whose device has a username on another date. _(Met except forward/reverse identity: 3 rows of one lent device differ by username; see the Comments.)_
- [x] Solution builds clean with 0 warnings; `Domain.Tests`, `Data.Tests` and the host `Tests` pass. (Regression and Reports suites: pre-existing state per ticket 20 / reports ticket 1 — say what changed, not what was already failing.)

## Out of scope

- The Dashboard section — that is reports ticket 22.
- Per-(device, day) attribution for lent devices (Decisions to confirm 4).
- Any change to how Field Maps devices are detected or attributed (ticket 20).

## Comments

Implemented test-first at the parser, aggregator, repository/schema and reporter seams; the shared code was extracted rather than copied, with ticket 20's behaviour and tests unchanged apart from mechanical renames.

**Domain** (`src/IisLogParserArcGIS.Domain/Aggregation/`): new `Survey123DeviceIdParser` (contains `AppFramework/` and `Survey123`, case-insensitive; first `(` after `AppFramework/`, first `)` after it; last `;+` item of that group; `Guid.TryParseExact(..., "N")`; stored as `guid.ToString("N")`, i.e. lowercase 32-hex, no hyphens). `BySurvey123DeviceAggregator` and `BySurvey123DeviceAggregateRow` are thin. The stage-1 logic (filter to the local date, tally per device, RAM-only login list with earliest-successful-login-by-timestamp wins, stamp usernames, keep `NULL`) moved out of `ByFieldMapsDeviceAggregator` into an internal `DeviceUsageTally` that takes the app's device-id rule as a `DeviceIdParser` delegate, and both aggregators now call it. It is split in two calls (`ExtractDeviceRequests`, `Tally`) to stay within the 3-argument `CC0042` limit. Renamed, not copied: `FieldMapsLoginParser` -> `LoginLineParser` (it was never Field-Maps-specific) and `FieldMapsAttributionCounts` -> `DeviceAttributionCounts`, which now takes rows through a small `IUsernameAttributedRow` interface that both row records implement.

**Data**: new table `aggregated_by_survey123_device` and index `ix_survey123_device_device_id` in `AggregateDatabaseSchema`; `AggregateTableNames.BySurvey123Device` (`All` is ten); `DailyAggregateBatch.BySurvey123Device`; `DailyAggregateReplacer` deletes/inserts it per local date and runs its back-fill inside the same transaction. The insert/delete/select SQL and the `MATERIALIZED`-CTE back-fill (ticket 20's Comments) now live once in `DeviceAggregateRepositoryBase<TRow>`, parameterized by table name; `ByFieldMapsDeviceRepository` and the new `BySurvey123DeviceRepository` are each a table name plus a static `BackfillUsernames`. The Field Maps SQL is unchanged apart from the table name now being interpolated.

**Host**: `Program.cs` aggregates Survey123 and logs a `Survey123 attribution:` line per harvest via `RunSummaryReporter.ReportSurvey123Attribution` (same Warning level and shape as the Field Maps line).

**Docs**: `README.md` (nine -> ten, ER diagram, table row, class diagrams, new "Domain: Survey123 username matching" section), `CONTEXT.md` (new **Survey123 Device**; **Daily Batch** back-fill exception covers both tables; **Login Line** no longer Field-Maps-only, method not examined), `docs/adr/0002` (nine -> ten).

**Real-corpus spot-check** (checked-in 2026 corpus, `America/Edmonton`, real CLI `harvest` once per local date, 2026-01-02 through 2026-09-09 = 251 dates, into two scratch databases, one forward and one in reverse date order):
- **5,841 Survey123 hits across 221 devices** (508 device-days). The 38 fewer hits than the ticket's 5,879 fall on the two boundary local dates that were not harvested (same effect as ticket 20's 41), and the ticket's 221 devices is matched exactly.
- **3,228 hits (55.3 %) attributed, 2,613 `NULL`** (the ticket's expected `NULL` figure exactly; 3,228 vs. the expected ~3,266 is the same 38 boundary hits). 135 devices attributed, 124 distinct usernames, **4 devices with more than one username**, no upper-case device ids or usernames, every device id 32 lowercase hex, and **no `NULL` row whose device has a username on another date** (checked in both databases).
- Summed per-harvest `Survey123 attribution:` lines, forward order: 508 device-days = 290 same-day + 35 back-filled at harvest time + 183 unattributed (reverse order: 290 / 42 / 176; the split depends on harvest order, as noted for Field Maps).
- Field Maps was re-checked in the same runs and is unchanged: 22,497 hits, **4,837 attributed / 17,660 `NULL`**, identical between forward and reverse, exactly as in ticket 20.

**Forward and reverse databases are NOT byte-identical for Survey123, and this is a consequence of the design, not a bug.** Field Maps is identical; Survey123 differs in exactly **3 rows (8 hits), all on one lent device** (`6efac5ac...`, cathy / garrett), and only in `username`. That device has a login by each user on different dates and `NULL` usage rows in between. Whichever login is already in the database when the `NULL` rows are back-filled donates its username, and "never overwrite" then keeps it, so forward order gives one user and reverse order the other. The ticket's acceptance criteria ask for both "never overwrites" and "forward and reverse order producing identical databases"; for a device with a single username they agree (and are tested), but for a lent device they cannot both hold without an extra column recording whether a username was inherited. That is the per-(device, day) attribution already ruled out of scope by *Decisions to confirm* 4, so I kept "never overwrite" and documented the caveat in the README. The other three lent devices (`544ab5b4`, `23bb75e5`, `e188d474`) happened to end identically in both orders. The mismatch touches 3 of 508 rows.

**Tests**: `Domain.Tests` 250 (new: `Survey123DeviceIdParserTests`, `BySurvey123DeviceAggregatorTests`, two login-stem cases and a Survey123 case in the counts tests), `Data.Tests` 161 (new: Survey123 repository round trip and delete scoping, the same back-fill property tests against the Survey123 table, schema nullable-username/index and ten-table checks, replacer tests for the ten tables and for Survey123 back-fill in both harvest orders without touching the Field Maps table), host `Tests` 62 (Survey123 attribution reporter). `RegressionTests` (4) and `Reports.Tests` (134) also pass unchanged in the full-solution run, so nothing regressed there. All pass and the solution builds with 0 warnings.
