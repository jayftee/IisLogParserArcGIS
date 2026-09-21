# 25 — Refuse to replace a day's aggregates when any of its log files was skipped

**What to build:** `LogCorpusParser` skips a whole log file when it cannot be read (e.g. locked by IIS) or when a header block lacks a required field, logs it, counts it in `SkippedFileCount` - and nothing read that count. `DailyAggregateReplacer.Replace` then deleted the target date's rows in every aggregate table and inserted only what the *remaining* files produced, and the run exited `0`. With one of a day's files skipped the day silently became partial; with **every** file skipped it was silently emptied, wiping previously good data. Found by the code review after ticket 24.

**Blocked by:** 22 (the check lives in `HarvestRunner`, extracted there).

**Status:** done

## Behavior

After parsing, if `SkippedFileCount > 0` the harvest stops before aggregating or touching the database rows: exit `1`, stderr

`Refusing to replace <yyyy-MM-dd>: <n> of <total> log file(s) were skipped (unreadable, or missing a required field), so the data would be incomplete. The existing data for that date was left untouched; see the log for the reason each file was skipped.`

Strict on purpose: a *partial* day is treated like a failed day, not a smaller day. For `harvest-regenerate` the Dashboard is not regenerated. The per-file reasons are already logged by `LogCorpusParser` / `LogFileParser` at error level, so the message points at the log rather than repeating them. Line-level skips (`Invalid++`) are unchanged: one bad line still only loses itself.

## Acceptance criteria

- [x] One of the day's files skipped (missing required fields) after a good harvest: exit `1`, message with `1 of 2`, the day's existing rows unchanged.
- [x] Every file of the day skipped: exit `1`, message with `1 of 1`, the existing rows **not** wiped.
- [x] `harvest-regenerate` with a skipped file: exit `1`, no Dashboard generated.
- [x] A file that cannot be read (opened exclusively by another handle) counts as skipped and is refused the same way.
- [x] No skipped files: behavior unchanged; the regression suite passes unmodified. Checked the real `2026/` corpus: all 1,273 `#Fields` header blocks contain every required field, so no real date is affected.
- [x] Build with 0 warnings; Host and Regression suites pass.

## Out of scope

- Retrying a locked file, or an option to harvest despite skipped files. If a legitimate need appears (harvesting today's still-open log), add an explicit flag rather than loosening the default.
- Skipping at the parser level (`LogCorpusParserTests` keep pinning "skip and count").

## Comments

Tests first (all four red against the old behavior, then green): `Run_OneOfTheDaysFilesIsSkipped_…`, `Run_EveryFileOfTheDayIsSkipped_…`, `Run_HarvestRegenerate_WhenAFileIsSkipped_…` and `Run_AFileCannotBeRead_…` in `HarvestRunnerTests`. The "existing rows survive" tests harvest a good day first, then add or corrupt a file and harvest again. README's whole-file-skip description still holds (the file is skipped and logged); what changed is the run's consequence. Host suite 109 -> 113.
