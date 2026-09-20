# 13 — Atomic daily replace across all seven tables

**What to build:** A single run's output fully replaces the target day's rows in every one of the seven tables as one atomic operation, so re-running a day never duplicates totals and a mid-run crash never leaves a half-updated day.

**Blocked by:** 06, 07, 08, 09, 10, 11, 12

**Status:** done

- [x] A single database transaction deletes existing rows for the target `local_date` across all seven tables, then inserts the freshly computed rows, committed together
- [x] Running the program twice in a row for the same local date leaves exactly one batch of rows per table, not doubled totals
- [x] `Data.Tests` simulate a failure partway through the replace and assert the prior day's rows are left completely untouched across all seven tables
- [x] No aggregate table is ever observable in a partially-replaced state from outside the transaction

## Comments

Extracted the previously inline transaction from `Program.cs` into `IisLogParserArcGIS.Data.Replacement`: `DailyAggregateBatch` (a record holding the target `LocalDate` plus all seven aggregate row collections) and `DailyAggregateReplacer.Replace(connection, batch, beforeCommit = null)`, which runs each table's delete-then-insert inside one `IDbTransaction`, rolling back and rethrowing on any failure. `Program.cs` now just builds a `DailyAggregateBatch` from the seven aggregators and calls `Replace` once. The optional `beforeCommit` hook exists solely so tests can inspect state from a second connection immediately before commit; production code never passes it.

`DailyAggregateReplacerTests` (`Data.Tests`) covers all four acceptance criteria against a real temp-file SQLite database and the real repositories (no fakes): a normal replace populates all seven tables; calling `Replace` twice for the same date leaves exactly one batch per table (no doubling); a deliberately invalid row (`Referer = null!` on the sixth of seven tables) throws a `SqliteException` partway through and the prior day's rows are asserted untouched in all seven tables afterward; and a `beforeCommit` hook opens a second connection and confirms the target day's rows are invisible there until after `Commit()` returns, while the prior day's rows remain visible throughout. Full suite green: 129 Domain, 31 Data, 44 Host tests passing. `/code-review` raised only that this ticket hadn't yet been closed out, which this comment addresses.
