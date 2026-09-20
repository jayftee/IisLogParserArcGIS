# 06 — By-URI aggregate

**What to build:** For a processed day, the database holds one row per normalized URI with accumulated hits and time-taken, queryable end-to-end from CLI run to database row.

**Blocked by:** 04, 05

**Status:** done

- [x] `uri_stem` is normalized and truncated to 1024 characters as the grouping key
- [x] Each aggregate row's `hits` equals the count of matching lines; `time_taken_second` is the sum of that group's time-taken values converted from milliseconds to seconds
- [x] Repository CRUD for this table follows the pattern established in ticket 05
- [x] `Domain.Tests` cover truncation at the 1024-character boundary
- [x] Running the program against a real day of logs populates this table with plausible, spot-checkable rows

## Comments

Implemented in `05cdcf3`: `ByUriAggregator.Aggregate` (`Domain/Aggregation/`) groups normalized requests by `uri_stem` truncated to 1024 chars, summing hits and time-taken (ms→s) per group; `ByUriRepository` (`Data/Repositories/`) supplies the three SQL strings over `AggregateRepositoryBase<ByUriAggregateRow>` per ticket 05's pattern; wired into `Program.cs`'s replace-on-run pipeline. `Domain.Tests` cover the 1024-character boundary (at/over/merge-on-truncation cases) plus date-filtering and null-argument guards. Full suite green: 35 Domain, 13 Data, 44 Host tests passing.
