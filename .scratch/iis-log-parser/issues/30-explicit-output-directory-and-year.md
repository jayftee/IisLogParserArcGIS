# 30 — Explicit output directory and year on every Dashboard-producing run; `.iis-dashboard` marker; explicit-date daily script; `.bat` templates

**What to build:** Stop the Dashboard output location and year from being implicit. Today `OutputDirectory` comes from `appsettings.json` and "this year" comes from the clock, so two runs against different databases (e.g. `Iis2025.sqlite` and `Iis2026.sqlite`, run in parallel) both write to the same Dashboard directory and the last to finish wins — and a Regeneration Run against a past year's database shows a 2026-to-date range that holds no data. Make both the output directory and the year explicit inputs, protect the output directory from being wiped by mistake, and replace the daily script's `-Yesterday` switch with an explicit target date.

**Blocked by:** none.

**Status:** done

## Decisions (from the owner, 2026-10-14)

- The output directory is a **required CLI argument** next to the database path — never prompted for interactively (scheduled runs are unattended) and never read from configuration. `OutputDirectory` is removed from `appsettings.json` and from `IISLOGPARSER_OutputDirectory`'s reach.
- The year is **always explicit** — never inferred from the clock. Where a command already takes a target Local Date, the year is that date's year; a separate year argument would only be a second copy that can disagree.
- Last 7 Days for any year other than the current one is empty. That is acceptable and needs no special case.
- The scripts take an explicit date. No `-Yesterday`; a caller that wants yesterday computes it. Task Scheduler launches a `.bat` wrapper, never PowerShell directly.

## Change

### CLI verbs

| Verb | Arguments |
| --- | --- |
| `harvest` | `<logSourceDirectory> <targetLocalDate> <outputDatabasePath>` — unchanged |
| `regenerate` | `<inputDatabasePath> <outputDirectory> --year <yyyy>` |
| `harvest-regenerate` | `<logSourceDirectory> <targetLocalDate> <outputDatabasePath> <outputDirectory>` — year taken from `<targetLocalDate>` |

- [x] `regenerate` and `harvest-regenerate` take `<outputDirectory>`; `OutputDirectory` is removed from `ReportsSettings`, `appsettings.json`, and the README/CONTEXT configuration docs.
- [x] `regenerate` takes a required `--year <yyyy>`; a missing or non-numeric/out-of-range year is an invalid-argument failure (non-zero exit, clear stderr), like any other bad argument.
- [x] `RegenerationRunBoundaries` is resolved from an explicit year instead of from the clock alone:
  - `YearStart` is January 1 of the year.
  - `Today` (the end of every "All" range) is the earlier of the clock's local today and December 31 of the year, so a past year's "All" covers the whole year and the current year's covers year-to-date as it does now.
  - `Last7DaysStart` stays `clock today − 6`, so for a past year the Last 7 Days range holds no rows.
  - The clock and `LocalTimeZone` still drive "today"; the year is the one new input.
- [x] `harvest-regenerate` derives the year from its `<targetLocalDate>` and passes it to the same code path.

### Output directory marker

- [x] A Regeneration Run writes an empty-ish marker file, `.iis-dashboard`, into the output directory as part of the Dashboard it generates.
- [x] Before deleting anything, a Regeneration Run proceeds only if the output directory does not exist, is empty, or contains `.iis-dashboard`. Otherwise it fails (non-zero exit, clear message naming the directory and the missing marker) and deletes nothing. The existing drive-root guard stays.
- [x] No override flag in this ticket (owner confirmed: they will empty their existing Dashboard folders before the first run after upgrading). A Dashboard generated before this change has no marker; say in the README that the old directory must be emptied by hand.

### Scripts

- [x] `Invoke-IisLogBackfill.ps1`: parameters `-LogSourceDirectory -OutputDatabasePath -OutputDirectory -Year`, all mandatory.
  - Iterates every Local Date of `-Year` (January 1 to December 31, or to today for the current year) instead of deriving dates from file names, harvests each in sequence, then runs `regenerate` once, unconditionally, as before.
  - This removes the script's "only correct when `LocalTimeZone` is UTC" caveat: each date is a Local Date handed to `harvest`, which already discovers the UTC files it needs.
  - A date whose files are missing is a recorded failure and the run continues (so December 31 fails in a per-year folder layout that lacks the next year's first file — expected, and reported).
  - Works unchanged for a log folder that holds many years: only the requested year's dates are attempted.
  - Update the comment-based help to drop the UTC caveat.
- [x] `Invoke-IisLogDailyRun.ps1`: parameters `-LogSourceDirectory -OutputDatabasePath -OutputDirectory -TargetDate <yyyy-MM-dd>`, all mandatory. `-Yesterday` is removed (breaking). It runs `harvest-regenerate` for exactly `-TargetDate`; its exit code is whatever `harvest-regenerate` returned. An unparseable `-TargetDate` fails before launching the executable.

### `.bat` templates

- [x] Three templates under `src/IisLogParserArcGIS/Scripts/`, copied next to the executable like the ps1 files: a backfill wrapper, an hourly wrapper (today), and a nightly wrapper (yesterday, for ~02:00). Each computes its own date (and year) with `powershell -NoProfile -Command`, builds the database file name from the **target date's** year (not the clock's — on January 1 the nightly run harvests December 31 of last year and must write to last year's database), passes the log source directory (per-year folder in the owner's layout), database path, and output directory, and exits with the script's exit code.
- [x] Fix the owner's existing production `IisLogDailyRun.bat`, which omits the mandatory `-LogSourceDirectory` (the script would fail on it): its replacement is the nightly/hourly template with the log source directory passed (`\\goa\backups\AgsBackup\PROD\ArchivedStatistics\WebAdaptor\<year>` in the owner's per-year layout). The shipped templates must always pass every mandatory parameter — acceptance criterion below checks this.
- [x] The templates use placeholder paths and say so in a header comment; they're starting points the operator edits.

## Acceptance criteria

- [x] `regenerate` without `<outputDirectory>` or without `--year` fails with a clear usage error; with both, it writes the Dashboard into `<outputDirectory>` and nowhere else.
- [x] Two `regenerate` runs against different databases and different output directories never touch each other's directory.
- [x] Regenerating a past year's database: every "All" range covers that year's data (not empty); every Last 7 Days range is empty; no page errors.
- [x] Regenerating the current year behaves as before.
- [x] Regenerate refuses a non-empty directory without `.iis-dashboard`, deleting nothing; accepts a missing, empty, or marked one; the marker is present after a successful run.
- [x] `harvest-regenerate` for a date in year Y behaves as `harvest` followed by `regenerate --year Y`.
- [x] `Invoke-IisLogBackfill.ps1 -Year Y` attempts exactly Y's Local Dates, never another year's, and reports per-date failures and a non-zero exit as before.
- [x] `Invoke-IisLogDailyRun.ps1` rejects a missing or unparseable `-TargetDate`.
- [x] Every `.bat` template passes every mandatory parameter of the script it calls (checked by a test that parses the template's `-Parameter` names against the script's mandatory `param()` block), so the missing-`-LogSourceDirectory` mistake can't recur.
- [x] README (CLI verbs table, Configuration table, backfill and scheduled-run sections), CONTEXT.md (Regeneration Run, Dashboard — output directory and year are inputs), and a new ADR (explicit output directory and year, marker file) are updated. Close this ticket's checkboxes and Status as part of finishing, and mark ticket 29's `-Yesterday` superseded.
- [x] Existing Reports, Regression, and CLI tests are updated to the new arguments; new tests cover the marker guard, the past-year boundaries, and the new argument validation. Build with 0 warnings; full suite passes.

## Out of scope

- A `--force` override for the marker guard.
- A multi-year Dashboard, or a year selector inside one Dashboard.
- Time-zone-aware handling beyond what `harvest` already does for one Local Date.
- Per-year subdirectory conventions for the output directory — the caller chooses the directory.

## Comments

Implemented 2026-10-14, test-first at each seam (boundaries, `RegenerationRun`, CLI validator/runners, scripts), then verified against the real compiled executable and the real 2026 corpus.

- **The ticket's year-derivation decisions, as built.** `harvest-regenerate` has no year argument (taken from `<targetLocalDate>`, `ParsedHarvestRegenerateArguments.Year`); `regenerate` has a required `--year`; `Invoke-IisLogDailyRun.ps1` takes `-TargetDate` only (no `-Year`); the backfill script takes `-Year`. Recorded in [ADR 0011](../../../docs/adr/0011-explicit-output-directory-and-year-with-dashboard-marker.md).
- **Year bounds.** The CLI accepts four-digit years (1000-9999) and rejects the rest as an invalid argument; a year after the current local year is refused later, by `RegenerationRunBoundaries.Resolve`, before the output directory is touched.
- **Last 7 Days for a past year** is empty by construction: `Last7DaysStart` stays the real clock's `today - 6`, which lies after `Today` (December 31 of that year), so every `>= Last7DaysStart` filter and `GetTop*(Last7DaysStart, Today)` query matches nothing. No section needed a special case; a past-year Dashboard over a database with no rows renders its "No data for this range." cards.
- **Marker guard lives in `RegenerationRun`**, not only in the CLI, so every caller (and the Reports test suite) gets it. It throws `ArgumentException` like the existing drive-root guard, which `DashboardRegenerator` already turns into "Failed to regenerate the Dashboard: ..." and exit 1.
- **Old tests that configured `OutputDirectory` through `appsettings.json`** now pass the directory as an argument; the cases that were relative to the executable's directory (`.`, `..`, `Logs`) became absolute `{base}`/`{root}` placeholders, since a CLI path is resolved against the current directory. A test records that a leftover `OutputDirectory` key in a configuration file is ignored.
- **`.bat` templates** are `IisLogBackfill.bat [year]`, `IisLogHourly.bat`, `IisLogNightly.bat` in `Scripts\`, copied next to the executable. `.gitattributes` gains `*.bat text eol=crlf`. `ScriptTemplateTests` parses each template's `-Parameter` names against the script's mandatory `param()` block (so the missing-`-LogSourceDirectory` mistake in the owner's old `IisLogDailyRun.bat` cannot recur) and runs the real scripts under `powershell.exe -NonInteractive` to check they reject a bad/missing `-TargetDate`, `-Year`, or the removed `-Yesterday`.
- **Verified for real:** the backfill `.bat` for 2026 over the 720-file corpus (287 calendar dates attempted through today; the 252 that have logs harvested, the rest reported as failed because the sample ends 2026-09-10) produced the same database size as the earlier backfill and a 3,114-file Dashboard including the marker; `regenerate` rerun into the marked directory, into a new directory for 2025, and refusals for an unmarked non-empty directory, a missing `--year`, a missing output directory, a non-numeric year and 2027 all behaved as specified.
- **Known limitation, documented in the nightly template:** with one log folder per year, the nightly run on January 1 harvests December 31, whose adjacent UTC day lives in the new year's folder, so that one day can only be completed by a backfill of the old year.
