# 08 — By-user-agent aggregate

**What to build:** For a processed day, the database holds one row per normalized user agent with accumulated hits and time-taken.

**Blocked by:** 04, 05

**Status:** done

- [x] User agent is normalized (`+` decoded to space, lowercased) and truncated to 1024 characters as the grouping key
- [x] `hits` and `time_taken_second` accumulate using the same rules as ticket 06
- [x] `Domain.Tests` cover `+`-decoding and the truncation boundary
- [x] Running the program against a real day of logs populates this table with plausible, spot-checkable rows

## Comments

Implemented in `85bc438`: `ByUserAgentAggregator.Aggregate` (`Domain/Aggregation/`) groups normalized requests by user agent with `+` decoded to space, lowercased, and truncated to 1024 chars, summing hits and time-taken (ms→s) per group, same shape as ticket 06's `ByUriAggregator`. `ByUserAgentRepository` supplies the three SQL strings over `AggregateRepositoryBase<ByUserAgentAggregateRow>` against the pre-existing `aggregated_by_user_agent` table. Wired into `Program.cs`'s replace-on-run transaction alongside by-URI and by-root. `Domain.Tests` cover `+`-decoding, lowercasing, and the 1024-character truncation boundary (at/over/merge-on-truncation cases), plus grouping/date-filtering/null-argument tests. Verified against the real 2026 log corpus for 2026-01-01: 34 user-agent rows, zero leftover `+` or uppercase characters, plausible top clients (Chrome browser, python-requests, ArcGIS Pro, mobile Safari). Full suite green: 56 Domain, 17 Data, 44 Host tests passing; `/code-review` found no Standards or Spec violations.
