# 07 — By-root aggregate

**What to build:** For a processed day, the database holds one row per root/site with accumulated hits and time-taken.

**Blocked by:** 04, 05

**Status:** done

- [x] Root is the first non-empty path segment of the URI stem, lowercased and truncated to 16 characters, with a `-` fallback when no segment is present
- [x] `hits` and `time_taken_second` accumulate using the same rules as ticket 06
- [x] Root normalization is implemented as a reusable Domain function, since ticket 12 (ArcGIS Server Service) reuses it for its `site` column
- [x] `Domain.Tests` cover the fallback case and the 16-character truncation boundary
- [x] Running the program against a real day of logs populates this table with plausible, spot-checkable rows

## Comments

Implemented in `b40e348`: `RootNormalizer.Normalize` (`Domain/Aggregation/`) is a standalone, reusable static function — first non-empty path segment, lowercased, truncated to 16 chars, `-` fallback — kept separate from `ByRootAggregator` specifically so ticket 12 can call it for the `site` column. `ByRootAggregator.Aggregate` groups normalized requests by root, summing hits and time-taken (ms→s) per group, same shape as ticket 06's `ByUriAggregator`. `ByRootRepository` supplies the three SQL strings over `AggregateRepositoryBase<ByRootAggregateRow>` against the pre-existing `aggregated_by_root` table. Wired into `Program.cs`'s replace-on-run transaction alongside by-URI. `Domain.Tests` cover the fallback (no segment / root-only path) and 16-character truncation boundary (at/over) cases, plus grouping/date-filtering/null-argument tests. Verified against the real 2026 log corpus for 2026-01-02: 15 root rows with hits summing to 1974, matching the by-URI total exactly. Full suite green: 48 Domain, 15 Data, 44 Host tests passing; `/code-review` found no Standards or Spec violations.
