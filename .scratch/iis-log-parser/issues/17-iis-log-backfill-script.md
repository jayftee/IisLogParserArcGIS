# 17 — `Invoke-IisLogBackfill.ps1` batch helper script

**What to build:** A PowerShell script, shipped alongside the compiled executable, that runs the CLI once per UTC calendar date found in a log source directory — so an entire year (or any accumulated backlog) of IIS logs can be processed into one output database without modifying the CLI program itself. The CLI stays single-date-per-invocation; this script is purely an orchestration layer around it.

**Status:** done

- [x] New file `src/IisLogParserArcGIS/Scripts/Invoke-IisLogBackfill.ps1`, wired into `IisLogParserArcGIS.csproj` with `<None Include="Scripts\Invoke-IisLogBackfill.ps1" CopyToOutputDirectory="PreserveNewest" />` — the same idiom already used for `appsettings.json` and `appsettings.Development.json` in that project. No other build changes (no post-build target, no publish profile — this repo builds via `dotnet build` only).
- [x] The script takes exactly two parameters: `LogSourceDirectory` and `OutputDatabasePath`. There is no date parameter — the whole point of this script is that it derives every date to process itself.
- [x] Date derivation: scan `LogSourceDirectory` for files matching `u_ex<YYMMDD>*.log` — the same opaque-wildcard glob as `LogFileLocator` (ticket 16). The script must never detect, parse, or branch on what follows the date (no `_x`/site-ID awareness), matching the "filename carries no schema information" principle from ticket 16. Extract the six-digit UTC date from each matching filename, de-duplicate, and sort ascending.
- [x] For each date in that sorted list, in strict sequence (never parallel — every invocation writes to the same shared SQLite output file, which is single-writer), invoke the compiled `IisLogParserArcGIS.exe` sitting next to this script, passing that date as its `targetLocalDate` argument along with the same `LogSourceDirectory`/`OutputDatabasePath` values.
- [x] The file's own embedded UTC date is passed straight through as the CLI's Local Date — no time-zone adjustment, no adjacent-UTC-date expansion. This is a deliberate, accepted limitation: it's only correct when the configured `LocalTimeZone` is `UTC` (today's `appsettings.json` setting), and even then, lines that cross midnight in the very first or last physical file the script finds are not attributed to any run (there's no file for the date on the far side of that boundary). This is a known, accepted characteristic of the script, not a bug to fix here.
- [x] Progress output: before each invocation, print `[n/total] Processing <date>...` (or equivalent) so a long run shows where it is.
- [x] Failure handling: if one date's invocation exits non-zero, record it and continue to the next date rather than aborting the whole run.
- [x] End-of-run summary: after the loop, print how many dates succeeded and how many failed (listing the failed dates). The script's own exit code is non-zero if any date failed, zero if every date succeeded.
- [x] PowerShell 5.1-compatible syntax throughout (no `pwsh`-only constructs) — this repo's Task Scheduler deployment target's PowerShell version isn't confirmed, and 5.1-compatible syntax also runs fine under `pwsh`.
- [x] `README.md` gains a short section describing how to run a backfill with this script, alongside the existing single-date "Run" section.
- [x] No `CONTEXT.md` changes — this doesn't introduce a new domain term, just an operational script.

## Comments

Raised after implementing ticket 16, when discussing operational needs: this deployment sits behind a load balancer with multiple IIS instances (already handled correctly for a single date — file discovery merges every site-ID's file for a given date), but there was no way to (re)process a full backlog of historical dates without calling the CLI once per day by hand. Deliberately scoped as a wrapper, not a CLI change, to keep the CLI's single-date contract (and its existing tests/regression suite) untouched.

Design decisions from grilling, for context:
- **Two args, not three**: the date argument is dropped entirely rather than kept-and-optional, since this script's only reason to exist is the "no date given" case.
- **Sequential execution is load-bearing, not a style preference**: `DailyAggregateReplacer` does one atomic transaction per date (`DELETE ... WHERE local_date = @LocalDate` then insert, per repository), so concurrent invocations against the same SQLite file risk lock contention/corruption for no benefit — each invocation is already fast.
- **Continue-on-failure was chosen deliberately** over fail-fast: a batch job processing a year of dates that dies on day 40 and has to be manually restarted defeats the purpose of the script, especially since each date's write is independently safe to skip and retry later (the CLI's per-date replace is idempotent).
- **The UTC-only/no-adjacent-date-expansion limitation was discussed and explicitly accepted**, not overlooked: it was raised as an open question during design, and the project owner confirmed the boundary-loss consequence (a few cross-midnight lines at the very edges of whatever's in the folder) is immaterial for this use case.

Implemented as scoped. `/code-review` caught three real issues before commit, all fixed:

- The date-parsing block used a throwing `[datetime]::ParseExact` with no guard, so one malformed/impossible date embedded in a filename (e.g. a manual rename) would crash the entire batch instead of just that file — directly against the ticket's own continue-on-failure rationale. Switched to `TryParseExact`, skipping the offending file with a warning.
- The file match relied on `Get-ChildItem -Filter 'u_ex*.log'`, which is subject to the Win32/NTFS 8.3 short-filename over-matching quirk (unlike `LogFileLocator.cs`, which explicitly guards against exactly this). Replaced with a plain file listing plus a single end-anchored regex (`^u_ex(\d{6}).*\.log$`) that both selects genuine `.log` files and extracts the date in one step.
- `ParseExact($rawDate, 'yyMMdd', ...)` relied on .NET's default two-digit-year pivot (ambiguous from 2050 onward). Fixed by explicitly prepending `"20"` and parsing as `yyyyMMdd`, removing the ambiguity entirely rather than just deferring it.

Verified end-to-end against the real compiled executable (not just read through) under both Windows PowerShell 5.1 and `pwsh`:
- Happy path: a fixture with a bare `u_ex<date>.log`, a `u_ex<date>_x.log`, and a `u_ex<date>_x_<siteId>.log` (three of the four filename shapes ticket 16 made valid) across two dates — confirmed same-day files merge into one run, dates process in ascending order, and both dates' rows land in the same output database (queried directly).
- Failure continuation: a sandboxed copy of the build output with `LocalTimeZone` set to a real negative-offset zone and a fixture missing specific adjacent-day files, deliberately engineered so the 1st and 4th of four dates hit a genuine `NoLogFilesFoundException` from the CLI while the 2nd and 3rd succeed — confirmed the loop keeps going past the first failure, the end summary lists exactly the right failed dates, the exit code is 1, and only the two successful dates' rows exist in the output database.
- Regression coverage for the three code-review fixes: a malformed-date filename (`u_ex269999.log`) is skipped with a warning rather than crashing the run; a wrong-extension file (`u_ex260101.logbak`) is correctly excluded from the discovered date set.

Full solution test suite (218 tests, unaffected by this change since it's PowerShell + csproj/README only) passes. One pre-existing, unrelated flake was observed in `IisLogParserArcGIS.Data.Tests` (intermittent `ObjectDisposedException` on the native SQLite handle, a different test each time it occurs) - not touched by or related to this ticket.
