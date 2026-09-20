/mattpocock-skills:implement # 11 — By-referer+URI aggregate

**What to build:** For a processed day, the database holds one row per (referer, URI) pair with accumulated hits and time-taken, composed from the normalizations already built.

**Blocked by:** 06, 09

**Status:** done

- [x] The grouping key composes the URI normalization from ticket 06 and the referer normalization from ticket 09, without re-deriving either
- [x] `hits` and `time_taken_second` accumulate using the same rules as ticket 06
- [x] `Domain.Tests` cover a case where the same URI appears under two different referers and vice versa, proving the pair (not either field alone) is the key
- [x] Running the program against a real day of logs populates this table with plausible, spot-checkable rows

## Comments

- Ticket 06 (`ByUriAggregator`) had no reusable normalizer to compose, unlike ticket 09's `RefererNormalizer`, so extracted a new `UriStemNormalizer` (1024-char truncation, mirroring `RootNormalizer`/`RefererNormalizer`) and refactored `ByUriAggregator` to use it — same behavior, now shared.
- `ByRefererAndUriAggregator.Aggregate` groups by the `(RefererNormalizer.Normalize(...), UriStemNormalizer.Normalize(...))` tuple; `ByRefererAndUriRepository` follows the established `AggregateRepositoryBase<TRow>` pattern over the pre-existing `aggregated_by_referer_and_uri` table; wired into `Program.cs`'s replace-on-run transaction.
- `Domain.Tests` cover: same-URI-under-two-referers and same-referer-under-two-URIs (each asserting 2 separate rows, proving the pair is the key), composed referer/URI normalization, date filtering, and the null-argument guard. Full suite green: 103 Domain, 23 Data, 44 Host tests passing.
- Spot-checked against `2026/u_ex260101_x_*.log`: 622 distinct (referer, URI) rows totaling 1938 hits, matching the by-URI aggregate's total hits exactly; confirmed rows where a single URI splits across multiple referers (e.g. `/titan/rest/info` under 4 referers) and a single referer spans multiple URIs (e.g. `https://landregistry.alberta.ca/` across 268 URIs).
