# 26 — Replace the raw table-name string with a `DeviceAggregateTable` type

**What to build:** The three per-device queries take a bare `string tableName` that is interpolated straight into SQL (`FROM {tableName}`), guarded only by a `<param>` doc line saying "one of the `AggregateTableNames` constants". `DeviceSection.TableName` and `DeviceUsernameSql.Expression(string tableName)` carry the same bare string. Review finding: Standards 3 (Primitive Obsession). **Low priority**: the values are compile-time constants today, so there is no exploitable path — this makes an invalid table unrepresentable instead of relying on a doc comment, and matters mainly because the query constructors are public API.

**Blocked by:** —

**Status:** done

## Approach (recommended, not prescriptive)

A small `DeviceAggregateTable` type in `IisLogParserArcGIS.Data.Schema`, with a private constructor, a `Name` property, and exactly two instances — `DeviceAggregateTable.FieldMaps` (`AggregateTableNames.ByFieldMapsDevice`) and `DeviceAggregateTable.Survey123` (`AggregateTableNames.BySurvey123Device`) — so no caller can construct one for an arbitrary string. Then:

- `DeviceTopHitsQuery`, `DeviceRangeTotalsQuery`, `DeviceDetailDailyHitTotalsQuery` take a `DeviceAggregateTable` instead of `string tableName`.
- `DeviceUsernameSql.Expression` takes one too.
- `DeviceSection.TableName` becomes `DeviceSection.Table` of that type; the builders pass it to the queries and use `Table.Name` for `AggregateDatabaseSchema.TableExists`.
- `AggregateTableNames` and the schema/repositories are untouched (they are the store's own vocabulary; `DeviceAggregateRepositoryBase.BackfillUsernames` also takes a table name, but changing the write side is out of scope).

## Acceptance criteria

- [x] No public constructor or method on the three queries, `DeviceUsernameSql` or `DeviceSection` accepts a free-form table-name `string`; no code path can pass an arbitrary string into the interpolated SQL.
- [x] Only the two device tables can be expressed as a `DeviceAggregateTable`.
- [x] Generated SQL is unchanged (same statements, same table names): every existing Data and Reports test passes with only the mechanical constructor-argument change in the Data query tests.
- [x] Build clean with 0 warnings; Data and Reports tests pass.

## Out of scope

- Changing `AggregateTableNames`, the schema, or the write-side repositories.
- Test reorganisation (ticket 28 depends on this ticket's constructor signatures).

## Comments
Implemented in bfe95de. New `DeviceAggregateTable` in `IisLogParserArcGIS.Data.Schema`: a sealed class with a private constructor, a `Name`, and exactly two instances, `FieldMaps` and `Survey123`, so no caller can build one for an arbitrary string. `DeviceTopHitsQuery`, `DeviceRangeTotalsQuery`, `DeviceDetailDailyHitTotalsQuery` and `DeviceUsernameSql.Expression` now take it instead of `string tableName`, and `DeviceSection.TableName` became `DeviceSection.Table`; the three builders pass `section.Table` to the queries and `section.Table.Name` to `AggregateDatabaseSchema.TableExists` (which already binds the name as a parameter, so it stays string-typed). `AggregateTableNames`, the schema and the write-side repositories are untouched. Test-first: `DeviceAggregateTableTests` (4 tests: each instance's name, both are real aggregate tables and distinct, no public constructor). The existing Data query tests changed only mechanically (`AggregateTableNames.X` to `DeviceAggregateTable.X` in the constructor call); no Reports test was modified. Full solution builds with 0 warnings; Domain 250, Host 62, Data 169, Regression 4, Reports 159, all passing.
