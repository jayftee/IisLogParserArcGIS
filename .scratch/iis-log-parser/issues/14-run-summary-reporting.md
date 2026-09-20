# 14 — Run summary reporting (end-to-end wiring)

**What to build:** A full, real invocation of the program — real CLI args, real log directory, real output database — runs start to finish and prints an accurate summary, proving the whole pipeline works together.

**Blocked by:** 13

**Status:** done

- [x] End-of-run summary reports total lines read, valid-line count, invalid/skipped-line count, and elapsed processing time
- [x] Summary counts are internally consistent (valid + invalid = total read) and match what was actually persisted
- [x] Running against a real day from the `2026/` corpus completes successfully, with the summary and the resulting database rows agreeing with each other
- [x] The zero-files-found hard failure from ticket 03 still reports correctly now that the full pipeline exists around it

## Comments

Added `IisLogParserArcGIS.Logging.RunSummaryReporter`: takes the existing `LogLineCounts` (already tracked per-run by `LogCorpusParser`/`LogFileParser`) plus an elapsed `TimeSpan`, and logs one Warning-level line — `Run summary: {Total} line(s) read, {Valid} valid, {Invalid} invalid/skipped, elapsed {Elapsed}s.` — following the exact optional-`ILoggerFactory`/`NullLoggerFactory` fallback shape from ADR 0003, mirroring `StartupAnnouncer`. `Program.cs` wraps a `Stopwatch` around file discovery through the daily replace and calls `Report` once that completes successfully.

Internal consistency (`valid + invalid = total`) was already guaranteed by the pre-existing `LogLineCounts` type; this ticket only had to wire real counts and elapsed time through to a printed summary. "Match what was actually persisted" holds at the bookkeeping level — the same `parseResult.Requests` list feeds every aggregator that builds the persisted `DailyAggregateBatch`, so there's no double-counting or dropped-request bug. One real-corpus wrinkle, confirmed while testing against `2026-01-01`: the summary's valid-line count (1992) came out higher than the sum of `hits` across the six non-ArcGIS tables (1938) — traced to 54 lines inside the `u_ex260101_*.log` files whose own `date` field reads `2025-12-31` (verified by grep on the raw files). Every aggregator filters to the target `local_date` by design (from tickets 06/07/etc.), so those lines are correctly counted as "valid" for the run but correctly excluded from that day's aggregate rows — pre-existing, intentional behavior, not a bug this ticket introduced or needed to fix.

Initially the reporter also wrote a duplicate plain-text line directly to `Console.Out` (via an injectable `TextWriter`), reasoning that a black-box regression test would want to grep stdout without depending on the Serilog format. `/code-review`'s Spec pass caught that this was unrequested and produced two differently-formatted copies of the same message on a real console run — Serilog's console sink already writes every `Warning` line to stdout (see `SerilogLoggerFactoryBuilder`), exactly how `StartupAnnouncer` gets console visibility today. Removed the `TextWriter` parameter and direct write entirely; the reporter now only logs, matching the established single-sink convention. A future black-box test can still match on the `Run summary: ...` substring inside the timestamped log line.

Verified manually against the real `2026/` corpus: `2026-01-01` (`LocalTimeZone=UTC`, zero offset) completes with exit code 0 and prints the summary once; `2050-01-01` (no matching files) still exits 1 with the "No log files found" message before any parsing, unchanged from ticket 03. `dotnet build`: 0 warnings/0 errors across all three `src/` projects. `dotnet test`: 129 Domain, 31 Data, 47 Host — all green.
