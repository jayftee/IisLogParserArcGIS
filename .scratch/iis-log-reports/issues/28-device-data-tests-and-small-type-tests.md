# 28 — Data-layer device query tests over both tables, and tests for the small device types

**What to build:** Two gaps the code review raised on the test side of the Data layer and the new types (Standards 1 — Data half and new types without tests; 5; 6).

**Blocked by:** 25, 26 (tests are written against `DeviceSection.All` and the `DeviceAggregateTable` constructor signatures)

**Status:** done

## 1. Data query tests: one set, run over both tables

`DeviceTopHitsQueryTests`, `DeviceRangeTotalsQueryTests` and `DeviceDetailDailyHitTotalsQueryTests` still only exercise the Field Maps table (they were mechanically adjusted in ticket 22), and `DeviceQueriesSourceTableTests` is a separate cross-cutting file that targets no single class (ADR 0004's 1:1 class-to-test-class naming) and has near-identical `FieldMapsRow` / `Survey123Row` helpers. It also exercises the daily-totals query in only one test.

- Make each of the three query test classes a `[Theory]` over both `DeviceAggregateTable` values (Fieldmaps, Survey123), inserting through the matching repository (`ByFieldMapsDeviceRepository` / `BySurvey123DeviceRepository`), so every existing behaviour is proved for both tables.
- Fold the isolation checks into those classes: for **each** query, an assertion that a row for the *same* device id in the *other* table is ignored (today the separate file checks table isolation for all three queries, but only the daily-totals test uses the *same* id in both tables, which is the stricter form to keep).
- Delete `DeviceQueriesSourceTableTests` and its duplicated row helpers.

## 2. Unit tests for the new types, mirroring the source tree

`DeviceSection`, `DeviceLabel` and `DeviceUsernameSql` have no test class of their own (covered only indirectly). Add, named 1:1 for the class:

- `DeviceSectionTests` (Reports): `DeviceSection.All` is exactly Fieldmaps then Survey123; each section's `LeaderboardHitsHref(All|Last7Days)`, `CompleteViewHref` and `DetailHref(id)` produce the documented paths (`fieldmaps/leaderboard-hits/all.html`, `survey123/detail/{id}.html`, …); a Detail Page href goes through `DetailPagePathSegment.Sanitize`.
- `DeviceLabelTests` (Reports): `username · deviceId` when attributed, the complete device id alone when `Username` is `null`, null device id throws.
- `DeviceUsernameSqlTests` (Data): earliest `local_date` wins, ties alphabetical, non-`NULL` rows only, looked up across the whole table rather than the queried range, for each device table (driven through the public query surface if these types stay `internal` and no `InternalsVisibleTo` exists for the test project; otherwise directly).

## Acceptance criteria

- [x] `DeviceQueriesSourceTableTests` no longer exists; each of the three query test classes runs every behaviour for both device tables, and each proves the other table's same-id device is ignored.
- [x] `DeviceSectionTests`, `DeviceLabelTests` and `DeviceUsernameSqlTests` exist and cover the behaviours listed above.
- [x] No test helper is duplicated per table.
- [x] Production code is untouched by this ticket (tests only).
- [x] Build clean with 0 warnings; Data and Reports suites pass.

## Out of scope

- The Reports section-level tests (ticket 27) and any production change.

## Comments
Implemented in e70b20e. **Data queries.** `DeviceTopHitsQueryTests`, `DeviceRangeTotalsQueryTests` and `DeviceDetailDailyHitTotalsQueryTests` are now `[Theory]`s over both `DeviceAggregateTable` values (new `DeviceTableTestSupport`: `TableNames` theory data, `TableFor`, `OtherTable`, one `DeviceRow` factory returning a new `DeviceTestRow`, and one `InsertRows` that goes through `ByFieldMapsDeviceRepository` or `BySurvey123DeviceRepository`). Each class gained an isolation test that puts the *same* device id, with a different username and different hits, in the other table and asserts it is ignored; `DeviceQueriesSourceTableTests` and its duplicated row helpers are deleted. **Small types.** `DeviceUsernameSqlTests` (Data; the SQL type is `internal` and there is no `InternalsVisibleTo`, so it is driven through both public queries that embed it, which must agree): earliest date wins even when a later name sorts first, same-date ties alphabetical, `NULL` rows ignored, a device with no username anywhere is unattributed, and the username is looked up across the whole table rather than the queried range, for each table. `DeviceSectionTests` (Reports): `All` is Fieldmaps then Survey123, each section's title/slug/table, the documented leaderboard, Complete View and Detail Page paths, and that `DetailHref` goes through `DetailPagePathSegment.Sanitize`. `DeviceLabelTests` (Reports): `DeviceLabel` is also `internal`, so the label rule (attributed `username · id`, unattributed id alone, two devices of one user stay distinct) is driven by calling the public `DeviceLeaderboardSectionBuilder` directly. Its "null device id throws" guard is unreachable through that path (a stored device id is never null) and so is not tested; the class sits in the harvested-database collection only so it stays serial with the other tests that open pooled SQLite connections.

**A latent test-infrastructure race, fixed as part of this ticket.** The extra Data tests made an existing race show up: `TempSqliteDatabase.Dispose` called the global `SqliteConnection.ClearAllPools()`, which, with test classes running in parallel, closed pooled connections other tests were still using (`ObjectDisposedException` inside `EnsureCreated`, a different unrelated test each time; 2 failures in 5 runs). It now clears only its own database's pool (`SqliteConnection.ClearPool` with the connection string the factory builds). 0 failures in 12 consecutive runs afterwards. `WorkingDatabaseCopy` and the other `ClearAllPools` callers in the Reports and Regression projects are untouched: they sit in serial collections. Production code is untouched. Full solution builds with 0 warnings; Domain 250, Host 62, Data 192, Regression 4, Reports 181, all passing.
