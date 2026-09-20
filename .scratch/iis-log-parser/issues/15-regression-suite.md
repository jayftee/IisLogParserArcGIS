# 15 — Regression suite against the full 2026/ corpus

**What to build:** An automated regression suite proves the shipped console executable, run as a black box against the full real-world log corpus, produces correct results despite that corpus's known schema drift, multi-header-block files, and site-ID rotation.

**Blocked by:** 14

**Status:** done

- [x] `IisLogParserArcGIS.RegressionTests` invokes the actual compiled console executable (not in-process Domain calls) with real CLI arguments against the full `2026/` corpus and a temporary SQLite output path
- [x] Assertions are made only on external behavior: process exit code, end-of-run summary text on stdout, and rows queried directly from the output database
- [x] The suite passes against the corpus's confirmed 16-vs-18-field schema drift without manual intervention
- [x] The suite passes across the corpus's multi-header-block files and rotating site IDs
- [x] The suite is runnable independently of the other three (synthetic, I/O-free) test projects

## Comments

Implemented `IisLogParserArcGIS.RegressionTests` (`tests/IisLogParserArcGIS.RegressionTests/`), a standalone xUnit project (only a `ProjectReference` to the console host, transitively pulling in Domain/Data) that launches the actual built `IisLogParserArcGIS.exe` as a child process against the real `2026/` corpus directory:

- `SchemaDriftRegressionTests` — 2026-01-01 (16-field header) and 2026-03-06 (the day's two files declare *different* field counts, 18 vs. 16, for the same UTC date).
- `MultiHeaderBlockRegressionTests` — 2026-06-08, where one site-ID file restarts its `#Fields` header block six times in a day.
- `SiteIdRotationRegressionTests` — 2026-05-11, where six files spanning three site-ID generations (10359/10360, 11042/11043, 391/392) are all active at once mid-rotation.

Each test asserts only external behavior: process exit code, the stdout run-summary line (parsed back via regex), and rows queried from the output SQLite database via the real Repositories — compared against an independent oracle (`RawCorpusLineCounter`) that re-derives expected line/hit counts straight from the raw files without going through any parsing code under test. `/code-review` (8 parallel angles) caught and fixed several issues before commit: the oracle didn't replicate the "a header missing a required field skips the whole file" rule; an `ArcGisService` hits-consistency assertion was tautological (true by construction) and passed vacuously on an empty result set (replaced with `Assert.NotEmpty`); an unused `ElapsedSeconds` field carried a latent culture-decimal-separator parsing bug (removed); executable discovery picked the newest-by-mtime exe across all build configurations rather than the one matching the test run's own Configuration/TFM (fixed to prefer an exact match); and the three test classes shared enough boilerplate to warrant a common `RegressionAssertions` helper. Full solution test suite (211 tests across all four projects) passes.
