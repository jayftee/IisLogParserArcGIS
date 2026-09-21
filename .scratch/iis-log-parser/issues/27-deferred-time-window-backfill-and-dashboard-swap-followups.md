# 27 — Deferred follow-ups: UTC-midnight lines, backfill script vs. time zone, atomic Dashboard rebuild, DST days

**What to build:** Four findings from the code review that followed tickets 22-24 that were deliberately **not** fixed in tickets 25/26/29 because each needs a decision from the owner first, or has been accepted. They are recorded here, with the evidence gathered so far, the options, and the concrete tasks each would need, so they can be picked up later without re-deriving anything. None of them blocks day-to-day operation with the shipped configuration (`LocalTimeZone` = `America/Edmonton`) except Part 2, which makes every backfill end with one failed date.

**Blocked by:** none. Parts are independent; suggested order if all are taken: 2, 3, then 1 (only if the time zone is ever set to UTC), 4 only if the accepted trade-off is ever revisited.

**Status:** deferred (needs an owner decision per part; see each part's "Decision needed").

| Part | Finding | Affects the shipped config? | Effort | Status |
|---|---|---|---|---|
| 1 | Lines stamped just before UTC midnight sit in the *next* day's file; not read when the offset is exactly zero | No (Edmonton is negative) | Small | Deferred (only matters for a UTC config) |
| 2 | `Invoke-IisLogBackfill.ps1` assumes `LocalTimeZone` = UTC; with Edmonton its last date always fails | **Yes** | Medium | Deferred, recommended first |
| 3 | `RegenerationRun` deletes the live Dashboard, then rebuilds it in place | Yes (on any failure) | Medium | Deferred |
| 4 | One fixed UTC offset per harvest day mis-buckets an hour on the two DST days a year | Yes, twice a year | Medium | **Accepted, no work planned** |

## Corrections to what the review reported

Checked against the code and the corpus while writing this; the original review text overstated two points.

- **Part 1 is narrower than reported.** The review said zero *or positive* offsets miss the adjacent day's file. Only an offset of exactly zero does; a positive offset already reads the previous UTC day's file, which is where its window's midnight-crossing lines are. See the table in Part 1.
- **Part 2 is a documented, accepted limitation, not a new bug.** Ticket 17 (`17-iis-log-backfill-script.md`) records "the file's own embedded UTC date is passed straight through as the CLI's Local Date - no time-zone adjustment ... only correct when the configured `LocalTimeZone` is `UTC` (today's `appsettings.json` setting)" and the script's own header says "an accepted characteristic of this script, not a bug". What changed is the premise: the shipped `appsettings.json` now says `America/Edmonton`, so the accepted-limitation reasoning no longer holds.

---

## Part 1 — Lines stamped just before UTC midnight live in the next day's file (offset exactly zero)

### Problem

`RequiredUtcDatesCalculator.Calculate(targetLocalDate, localUtcOffset)` returns `[d]` for a zero offset, `[d, d+1]` for a negative one and `[d-1, d]` for a positive one. `LogLineParser` turns each line's UTC timestamp into a local date with the same fixed offset, and the aggregators keep only lines whose local date is the target date.

The real logs contain lines whose date is *earlier* than their file's date. Measured over the whole checked-in `2026/` corpus (720 files, 513,097 data lines): **36,788 lines (7.2%) in 503 files carry the previous UTC day's date, none carry a later one, and all of them are in hour 23** - i.e. the last moments before midnight. Example: `u_ex260102_x_10359.log` has `#Date: 2026-01-02 00:00:01` and its first data line is stamped `2026-01-01 23:59`. (The corpus is sparse and dominated by hour 00, so 7.2% is not representative of production volume; the mechanism is what matters. The most likely cause is that IIS writes an entry after the request completes, so the last requests of one UTC day are written into the file the server has already rolled to the next day; not verified, and not needed for a fix.)

Consequence by offset at the target date, for target local day *d*:

| Offset | Window of UTC time for day *d* | Files read | Effect |
|---|---|---|---|
| Negative (shipped: Edmonton, -7/-6) | *d* 07:00Z/06:00Z -> *d*+1 07:00Z/06:00Z | *d*, *d*+1 | Fine: the UTC midnight is inside the window and the lines stamped *d* 23:xx are in file *d*+1, which is read. |
| Positive | *d*-1 (24-off):00Z -> *d* (24-off):00Z | *d*-1, *d* | Fine: lines stamped *d*-1 23:xx are in file *d*, which is read. |
| **Exactly zero** (`UTC`, or a zone at offset 0 on that date such as `Europe/London` in winter) | *d* 00:00Z -> *d*+1 00:00Z | *d* only | **Lines stamped *d* 23:xx sit in file *d*+1, which is not read: lost every day.** Lines stamped *d*-1 23:xx in file *d* are read but correctly attributed to *d*-1, whose own run does not read file *d*: lost as well. |

So only a zero-offset configuration loses data, a few lines per day boundary. That is also why the memory note "a `u_ex<date>` file can contain lines timestamped for the adjacent day" matters: it is true, and the current design only tolerates it because the shipped zone is negative.

### Options

- **A (recommended, if ever needed):** for a zero offset also read the next day's file: `Calculate` returns `[d, d+1]` for `offset <= 0`. Same cost as negative offsets already pay: the newest day cannot be harvested until the next day's first file exists.
- **B:** make the extra file optional (read it when present, warn when absent). Rejected: it silently produces a partial day, which is exactly what ticket 25 stopped.

### Tasks (only if the time zone is ever set to UTC / an offset-0 zone)

1. Confirm the mechanism against a full-day production log (not the sparse corpus): count lines per file whose date differs from the file's date, and their hours.
2. Change `RequiredUtcDatesCalculator` for `offset <= 0` and update its XML docs; update `RequiredUtcDatesCalculatorTests` (zero offset now returns two dates) and `LogFileLocatorTests` (zero offset with a missing next-day file now throws `NoLogFilesFoundException`).
3. Add an end-to-end test: a UTC-configured harvest of day *d* with a line stamped `d 23:59:xx` inside the *d*+1 file is counted (assert through the by-URI repository), and one stamped `d-1 23:59:xx` inside file *d* is not.
4. Update the README "file discovery" flowchart and its zero-offset branch, and `CONTEXT.md` if it describes the rule.
5. Re-check Part 2: the backfill script's UTC assumption interacts with this change (a UTC backfill's last date would then also need a next-day file).

### Decision needed

Whether a UTC (or any offset-0) configuration is ever expected. If the deployment stays on Edmonton, close this part as "not applicable".

---

## Part 2 — `Invoke-IisLogBackfill.ps1` assumes `LocalTimeZone` = UTC

### Problem

The script (`src/IisLogParserArcGIS/Scripts/Invoke-IisLogBackfill.ps1`) lists the UTC dates in the file names of a log directory and runs `harvest <dir> <thatDate> <db>` for each, passing the UTC date **as the local date**. With `LocalTimeZone` = `America/Edmonton` (negative offset), harvesting local date *d* needs UTC files *d* and *d*+1 (see Part 1's table), so:

- The **last** date in every backfill has no *d*+1 file: `harvest` fails with `NoLogFilesFoundException`, the script records a failed date and exits `1`. A perfectly healthy backfill therefore always ends "N-1 succeeded, 1 failed". The operator cannot tell this expected edge from a real failure. (Ticket 17's own verification even engineered failures on a negative-offset zone to test failure continuation, so the behavior is known, just no longer accepted by its premise.)
- The **first** file's first ~6-7 hours belong to the previous local day, which the script never harvests; the last local day is never harvested either. These partial edge days are unavoidable, but they should be reported as "incomplete edge dates, not harvested", not as failures, and a full-year folder should say so.
- The script cannot compute the right dates itself in Windows PowerShell 5.1 (the ticket-17 compatibility target): `[TimeZoneInfo]::FindSystemTimeZoneById('America/Edmonton')` needs an IANA-aware runtime (PowerShell 7 / .NET with ICU); on 5.1 the id would have to be a Windows id (`Mountain Standard Time`). So a script-only fix is fragile, and the same offset/adjacent-file logic already exists, tested, in C# (`LocalUtcOffsetResolver`, `RequiredUtcDatesCalculator`).

### Options

- **A - CLI verb that does the whole backfill (recommended).** A new verb (name to be decided, e.g. `backfill <logSourceDirectory> <outputDatabasePath>`; ADR 0008 says explicit verbs, no default) that in one process: lists the file UTC dates, uses the configured time zone with `RequiredUtcDatesCalculator` to find the local dates whose required files are all present, harvests each in order with the same logic as `HarvestRunner`, reports the un-coverable edge dates as informational, and (decision below) regenerates once at the end. The script becomes a thin wrapper or is retired; the logic is unit-testable like the rest of `Cli/`, no PowerShell version issue, one process/logger/DB open instead of one per date.
- **B - small interim fix.** Give `NoLogFilesFoundException` a distinct exit code (e.g. `2`, documented in the README verb table) and have the script treat exit `2` on the first/last date as "incomplete edge, skipped" (warning, not a failure), so the script's exit code is `0` for a healthy backfill. Cheap, but keeps the fragile date logic and per-process cost, and does not tell a *missing middle* file from an *expected edge*.
- **C - script computes local dates.** Read `appsettings.json` and resolve the offset in PowerShell. Rejected: IANA ids on 5.1, duplicated logic.

### Tasks (for option A; B is tasks 1, 6, 7 only)

1. Decide the verb name and arguments; record it in ADR 0008's list of verbs (amend or add an ADR note).
2. Extract the per-day harvest from `HarvestRunner.Execute` into a reusable unit so both `harvest` and the new verb share it (opening the connection/logger once and looping days inside; keep the ticket 24/25 behaviors: time zone checked before the database is created, skipped files refuse the day).
3. Add the date-set computation: distinct UTC dates from file names using the same wildcard rule as `LogFileLocator` (nothing after the date is inspected); for each, `RequiredUtcDatesCalculator` + `LocalUtcOffsetResolver` decide whether all required files exist; classify each date as *harvest*, *incomplete edge* (missing an adjacent file at the start or end of the range) or *gap* (a missing file in the middle: a real failure).
4. Report: per date progress, then a summary of harvested / failed / incomplete-edge dates. Exit `0` only if no gap and no failure; incomplete edges alone do not fail the run.
5. Decide the end-of-run regeneration rule (see below) and implement it via `DashboardRegenerator`.
6. Update `Invoke-IisLogBackfill.ps1` (delegate to the verb, or a documented exit-code contract for option B), its header comment (remove the "accepted characteristic" text) and the README backfill section and verb table.
7. Tests in `tests/IisLogParserArcGIS.Tests/Cli/`: healthy folder with a negative offset ends `0` with the last date reported as an incomplete edge; a missing middle file is a failure; a folder with one file; UTC configuration; invalid time zone; the existing "regenerate unconditionally even if every date failed" semantics whichever way it is decided. Manual check against the real corpus and the built exe.

### Decision needed

- A vs. B (and whether to retire the script).
- Ticket 19 fixed "regenerate unconditionally after the loop, even if every date failed", partly because `regenerate` needs the database to exist. Since ticket 24 an invalid time zone no longer leaves a database behind, and a "no log files" failure still does. Keep the unconditional regenerate, or regenerate only when at least one date succeeded?

---

## Part 3 — The Dashboard rebuild is not atomic

### Problem

`RegenerationRun.Run` (`src/IisLogParserArcGIS.Reports/RegenerationRun.cs`, around lines 47-52) does `Directory.Delete(outputDirectory, recursive: true)`, `Directory.CreateDirectory(...)`, and then every section builder writes its pages straight into that directory. Failure modes:

- Any builder throws part-way (`SqliteException`, `IOException`, a sanitized-path collision, disk full): the previous, working Dashboard is already gone and the site serves 404s or a partial tree until the next successful run.
- `Directory.Delete` itself can fail part-way when a file is open (IIS or a viewer holding a file on a shared folder), leaving a half-deleted tree.
- While the (multi-minute?) build runs, viewers see missing pages. The build duration on a production-size database has not been measured.
- The process being killed mid-run leaves the same broken state.

Hosting is the owner's own infrastructure (reports ticket 07: "opened from a plain shared folder or served by an IIS site"), so what a swap may do to the output directory is unknown here: renaming a directory that IIS uses as a site/virtual-directory root, or that has open handles, can fail.

### Options

- **A - build beside, then swap (recommended, if the hosting allows).** Build into a sibling `<output>.building` (same volume), then rename the current directory to `<output>.previous`, rename `.building` to `<output>`, delete `.previous`. Any failure before the swap leaves the live Dashboard untouched; a failure during the swap can be rolled back by renaming `.previous` back.
- **B - build beside, then mirror into the live directory.** Never remove the live directory: copy new/changed files over it and delete stale files afterwards. Works when the directory is a mount point, a UNC share or an IIS site root that cannot be renamed; the live tree is briefly a mix of old and new pages, but never empty, and a build failure leaves it fully old.
- **C - keep in-place rebuild, add a backup.** Copy the live directory aside first and restore on failure. Simplest, but a long window without a Dashboard remains.

### Tasks

1. Establish the hosting facts: is the output directory an IIS site physical path, a virtual directory, or a UNC share? Can the account create siblings next to it and rename it? Measure `harvest-regenerate` / `regenerate` duration on a production-size database.
2. Decide A, B or A-with-B-fallback (try the rename, fall back to mirroring on `IOException`/`UnauthorizedAccessException`).
3. Change `RegenerationRun` to build in the sibling directory and swap, with cleanup of a stale `.building`/`.previous` left by a crashed run at the start of the next one. Keep the existing "not a drive root" guard and make sure the temporary directory names cannot collide with anything real.
4. Interplay with the guard from ticket 23 (`DashboardRegenerator`): the protected-path checks must still run against the *final* output directory before any build begins, and the temporary directories must also be checked (they sit next to the output directory, so an output directory directly inside a protected one stays allowed).
5. Tests in `RegenerationRunTests` and `DashboardRegeneratorTests`: a builder failure (inject via a corrupt/locked input or a read-only target) leaves the previous Dashboard byte-for-byte intact; a successful run replaces it; a stale `.building`/`.previous` is cleaned; a failed swap is rolled back; pages that no longer exist do not survive a successful run.
6. Update the README ("regeneration is a full rebuild") and ADR 0005's consequences if the "replaced outright" wording changes.

### Decision needed

Hosting facts (task 1) and A vs. B. If the Dashboard is only consumed by people opening it during business hours and a failed run is rare, the whole part can stay deferred.

---

## Part 4 — DST days: one fixed UTC offset per harvest day (accepted)

### Problem (recorded, no work planned)

`LocalUtcOffsetResolver.Resolve` returns the zone's UTC offset **at local midnight of the target date**, and `LogLineParser` applies that single offset to every line of the day. On the two days a year the offset changes:

- **Spring forward (2026-03-08, Edmonton):** midnight is MST (-7), the day is 23 hours long. The harvest window is 03-08 07:00Z -> 03-09 07:00Z but the real local day ends at 03-09 06:00Z, so the hour 06:00-07:00Z on 03-09 is counted in 03-08's run **and** again in 03-09's run (whose offset is -6): **double-counted**.
- **Fall back (2026-11-01):** midnight is MDT (-6), the day is 25 hours long. The window is 11-01 06:00Z -> 11-02 06:00Z, but the real day ends at 11-02 07:00Z; the hour 06:00-07:00Z on 11-02 is in neither 11-01's window nor 11-02's (which starts at 07:00Z): **lost**.

Effect: one hour of traffic, twice a year. **Decision (owner, 2026-09-20): this does not matter; accepted.** It is stored in the project memory so reviews do not raise it again.

### If it is ever revisited

- Convert each line with `TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, zone)` (per-line offset) instead of adding a fixed offset; the local date of a line is then always right.
- `RequiredUtcDatesCalculator` must take the `TimeZoneInfo` and the local date, compute the UTC instants of the local day's start and end, and return every UTC date those instants span (plus the next day for the just-after-midnight lines from Part 1). `LocalUtcOffsetResolver` is replaced by that.
- Tests: `America/Edmonton` on 2026-03-08 and 2026-11-01 (23- and 25-hour days) with lines at every hour, asserting each hour is counted in exactly one run; `UTC` and a positive zone unchanged; an oracle comparing per-line conversion against the fixed-offset path on non-transition days (results must be identical).
- Touches the same code as Parts 1 and 2, so if any of those is done, consider doing this at the same time.

---

## Acceptance criteria

This is a holding issue. A part is complete when its own tasks are done and:

- [ ] Part 1: decision recorded (applies or closes as "not applicable"); if it applies, tasks 1-5 done and the zero-offset tests green.
- [ ] Part 2: decision recorded; a healthy Edmonton backfill of a full folder exits `0`, reports the edge dates as incomplete rather than failed, and a missing middle file is still a failure.
- [ ] Part 3: hosting facts recorded; a failed regeneration leaves the previous Dashboard intact.
- [ ] Part 4: nothing to do unless revisited.
- [ ] Every part that ships builds with 0 warnings and passes Domain, Host, Data, Regression and Reports suites, and updates the README/ADRs it touches.

## Out of scope

- Everything already fixed by tickets 22-26 and reports ticket 29 (CLI runners, deletion guard, skipped-file refusal, IPv6, Complete View HTML encoding).
- The review's unverified suspicions: URL-escaping of detail-page hrefs for `%`/`#` (`DetailPagePathSegment`/`DashboardSidebar`) and missing `local_date` indexes on the aggregate tables. Neither was reproduced or measured; open a ticket if a real symptom appears (a 404 on a detail link, or a slow harvest/regeneration on a production-size database).

## Comments

Written after the review that followed tickets 22-24. Evidence gathered while writing it: the corpus statistics in Part 1 (an awk pass comparing each data line's date with the date in its file name, over all 720 files); the ticket-17 and script-header wording quoted in Part 2; the hosting answer of reports ticket 07 for Part 3. Not gathered: any production timing for regeneration, the production hosting setup, and a full-day production log for Part 1 task 1.
