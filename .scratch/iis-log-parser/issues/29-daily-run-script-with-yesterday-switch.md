# 29 — `Invoke-IisLogDailyRun.ps1`: a Scheduled-Task wrapper with a `-Yesterday` switch

**What to build:** A PowerShell script, shipped alongside the compiled executable like `Invoke-IisLogBackfill.ps1`, for an unattended Scheduled Task that runs `harvest-regenerate` for exactly one day — today by default, or yesterday with a switch — so the task's own arguments never need editing as the calendar moves forward.

**Blocked by:** none.

**Status:** done (the `-Yesterday` switch was superseded by ticket 30: the script now takes an explicit `-TargetDate`, and the `.bat` wrappers compute today/yesterday)

## Change

- [x] New file `src/IisLogParserArcGIS/Scripts/Invoke-IisLogDailyRun.ps1`, wired into `IisLogParserArcGIS.csproj` with `<None Include="Scripts\Invoke-IisLogDailyRun.ps1" CopyToOutputDirectory="PreserveNewest" />`, the same idiom as `Invoke-IisLogBackfill.ps1`.
- [x] Takes the same two mandatory parameters as the backfill script (`LogSourceDirectory`, `OutputDatabasePath`) plus one optional switch, `-Yesterday`. No date parameter — the whole point is that the script computes it.
- [x] Computes the target date as `Get-Date` (today), or `(Get-Date).AddDays(-1)` when `-Yesterday` is passed, formats it `yyyy-MM-dd`, and calls `harvest-regenerate <LogSourceDirectory> <that date> <OutputDatabasePath>` exactly once - unlike the backfill script, it never scans the log directory for dates.
- [x] Exit code is whatever `harvest-regenerate` itself returned (`exit $LASTEXITCODE`), not a separately tracked success/failure list - there is only ever one invocation.
- [x] Comment-based help (`.SYNOPSIS`/`.DESCRIPTION`/`.PARAMETER`/`.EXAMPLE`) documents both intended Scheduled Task entries (hourly with no switch; once daily, shortly after midnight, with `-Yesterday`) and why hourly same-day re-runs are safe.
- [x] `README.md` gains a "Scheduled daily runs" section (after the backfill section) with both example invocations and the same rationale.
- [x] No `CONTEXT.md` change - an operational script, no new domain term.

## Out of scope

- A general `-DayOffset <int>` parameter. Considered and rejected: the only two real operating modes described are "today" and "yesterday," and a one-off manual catch-up for an arbitrary earlier date can already be done directly with the existing `harvest-regenerate <dir> <explicit-date> <db>` verb, without this script needing a general offset API.
- A `-Today` switch alongside `-Yesterday`. Two switches would either need explicit mutual-exclusivity validation (guarding against `-Today -Yesterday` together, a meaningless combination) or still fall back to a default when neither is given - so it doesn't remove the implicit-default case, it just adds a second way to reach it. `-Yesterday` alone, with absence meaning "today," keeps the invalid dual-selection state structurally impossible.
- Any change to the CLI itself, or to `Invoke-IisLogBackfill.ps1` - this is a new, separate, much simpler script (one computed date, one `harvest-regenerate` call) rather than a variant of the backfill script's own date-discovery loop, which solves a different problem (an unknown backlog of dates, not one known date).

## Comments

Raised directly by the operator while ticket 30 (Reports: Complete View TSV export/paging) was running its test suite in the background, after regenerating the Dashboard manually and noticing it needed a real, complete aggregate database. The operator wants this scheduled roughly hourly (no switch, to keep today's partial-day data fresh) and once more at 02:00 with `-Yesterday` (to pick up log lines IIS wrote right at the end of the previous day, after the last hourly run of that day already happened).

Design choices, decided in a short back-and-forth before implementing:
- **A new script, not an extension of the backfill script.** The backfill script's whole design - scan a folder for every date it contains, harvest each in a loop, regenerate once at the end - exists to handle an *unknown* backlog. The daily case already knows its one date, so it's a thin wrapper around the existing single-date `harvest-regenerate` verb instead.
- **`-Yesterday` switch, not `-DayOffset <int>` or a `-Today`/`-Yesterday` pair** - see Out of scope above for the reasoning on each.
- Mirrors the backfill script's PowerShell 5.1-compatible style (no `pwsh`-only syntax: no `?:` ternary, no `??`) for the same Task Scheduler deployment-target reason ticket 17 recorded.

Verified against the real compiled executable (not just read through): built the Cli project, confirmed the new script is copied to `bin/Debug/net10.0/Scripts/` alongside the backfill script, then ran it twice against the real `2026/` corpus directory - once with no switch and once with `-Yesterday` - and confirmed from the printed `harvest-regenerate` invocation's own log line that the target local date was today's and yesterday's real calendar date, respectively (both correctly failed with "No log files found," since the checked-in corpus's dates don't reach this far forward - that failure is the corpus's date range, not a script bug, and confirms the exit-code pass-through works). Scratch output files from this manual check were removed afterward (not committed; `bin/` is gitignored regardless).
