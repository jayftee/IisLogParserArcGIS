---
status: accepted
---

# The Dashboard's output directory and year are explicit inputs, and a marker file guards the directory

The output directory used to come from `appsettings.json` (`OutputDirectory`) and "this year" from the clock, while the aggregate database path was a per-run argument. Operators who processed two years at once, into `Iis2025.sqlite` and `Iis2026.sqlite`, therefore regenerated into the same Dashboard directory: whichever run finished last won, and since 2025 is far larger than 2026 and finished last, it replaced the 2026 Dashboard — with a mostly empty one, because the year ranges were still derived from the clock's year and the 2025 database holds no 2026 rows.

Both are now required inputs of every Dashboard-producing run. `regenerate <inputDatabasePath> <outputDirectory> --year <yyyy>`; `harvest-regenerate <logSourceDirectory> <targetLocalDate> <outputDatabasePath> <outputDirectory>`, which takes the year from `<targetLocalDate>` (a second copy of the year could only disagree with the first). `OutputDirectory` is gone from configuration. It is a plain argument, never an interactive prompt, because the scheduled runs are unattended.

A year is reported in full: `YearStart` is January 1 and the end of every "All" range is the earlier of the clock's local today and December 31. "Last 7 Days" stays the real last seven days, so it is empty for a year that is already over — accepted, not special-cased. A year after the current local year is refused.

A Regeneration Run deletes its output directory recursively, and with the path now typed by the operator a typo (`D:\`, `D:\Aggregates`) could delete real data. Every generated Dashboard therefore carries an empty `.iis-dashboard` marker, and a run only replaces a directory that is missing, empty, or holds the marker; anything else is refused with nothing deleted. This sits beside the existing refusals (drive root, the executable's own directory, the aggregate database, the log output and log source directories). There is deliberately no override flag: a Dashboard from before the marker existed is emptied by hand once.

The Scheduled Task launches a `.bat` wrapper, which computes the date it needs and takes the year of the per-year database and output paths from that date — not from the clock, which would put December 31's nightly harvest into the new year's database on January 1. `Invoke-IisLogDailyRun.ps1` therefore takes an explicit `-TargetDate` (its `-Yesterday` switch is removed), and `Invoke-IisLogBackfill.ps1` takes `-Year` and walks that year's Local Dates instead of deriving dates from log file names, which also removes its old "correct only when `LocalTimeZone` is UTC" caveat.
