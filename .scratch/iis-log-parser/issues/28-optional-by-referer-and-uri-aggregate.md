# 28 — Make the by-referer-and-URI aggregate optional (`ComputeByRefererAndUri`)

**What to build:** A new boolean setting, `ComputeByRefererAndUri`, in `appsettings.json`. When `true`, a Harvest Run computes and stores the by-referer-and-URI aggregate exactly as it does today. When `false`, the Harvest Run does not compute it and does not touch its table. The `aggregated_by_referer_and_uri` table is still created by `AggregateDatabaseSchema.EnsureCreated`, so the schema is the same either way, but with the setting off it stays empty.

**Why:** The production databases are large: `Iis2023.sqlite` 21.8 GB, `Iis2024.sqlite` 21.9 GB, `Iis2025.sqlite` 38.6 GB and `Iis2026.sqlite` 26.7 GB (about 100 MB a day), 109 GB in all, filling the server's D: drive. The 2026 Dashboard, 50,353 HTML pages, is only about 2.3 GB, so the databases are about 98% of the footprint. The by-referer-and-URI aggregate is the prime suspect: it stores one row per (referer, raw `cs-uri-stem` truncated to 1024 characters) pair per day, and scanner probes and URLs carrying tokens make most pairs unique.

Nothing reads this table. No Reports query or Dashboard page uses it: the four flat Leaderboards use the by-URI, by-referer, by-user-agent and by-forwarded-for-IP aggregates only. Only the Harvest Run writes it (`HarvestRunner` -> `DailyAggregateReplacer` -> `ByRefererAndUriRepository`). It is kept because `requirements.md` lists it as a wanted aggregate for the rare occasion someone needs it, so it is made opt-in rather than removed.

**Blocked by:** none.

**Status:** done (task 1, the `dbstat` confirmation on the server database, is still to do by the owner)

## Behavior

1. **Setting:** `ComputeByRefererAndUri` (bool) on `AppSettings`, bound like the other settings, so it is overridable per run with `IISLOGPARSER_ComputeByRefererAndUri`. A missing or blank key means `false`.
2. **Enabled (`true`):** unchanged. The day's rows are aggregated in memory, deleted for that Local Date and inserted in the same transaction as every other aggregate.
3. **Disabled (`false`):**
   - `ByRefererAndUriAggregator.Aggregate` is not called at all. This also skips building the per-day referer+URI dictionary in memory, which is a cost on its own.
   - `DailyAggregateReplacer` skips that table entirely: no delete, no insert. Rows from an earlier run with the setting on are left untouched. Decided by the owner.
   - The table still exists, and stays empty on a database that never had the setting on.
4. **Schema:** unchanged. `CREATE TABLE IF NOT EXISTS` still runs for all ten tables, so the Dashboard's `TableExists` checks and any external consumer see the same schema regardless of the setting.
5. The setting is read once per run by the harvest, alongside `PortalWebAdaptorName`. The regenerate-only verb does not need it, as the Dashboard never reads this table.
6. **Default (owner-confirmed):** `false` in the shipped `appsettings.json` and in the code default, since the stated goal is to stop growing the database and the data is rarely needed. This changes what an existing deployment writes on its next harvest, so the README must say so plainly.

## Suggested design

- `DailyAggregateBatch.ByRefererAndUri` becomes nullable: `null` means "not computed, leave the table alone". `DailyAggregateReplacer.Replace` only calls `ReplaceTable` for it when it is non-null. This keeps the decision in the batch rather than adding a flag to the replacer's signature.
- `HarvestRunner.Execute` sets it to `appSettings.ComputeByRefererAndUri ? ByRefererAndUriAggregator.Aggregate(...) : null`.
- Add the property to `AppSettings` with a summary in the same style as the others. It is a `bool`, so `AppConfigurationFactory.BindAppSettings` needs no `Coalesce` and the code default is `false`.

## Tasks

1. **Confirm before merging**, on a copy of `Iis2026.sqlite`: `SELECT name, ROUND(SUM(pgsize)/1073741824.0,2) FROM dbstat GROUP BY name ORDER BY 2 DESC LIMIT 12;`. If `aggregated_by_referer_and_uri` is not the dominant table, still do this ticket, but re-plan the disk-space goal (see Out of scope) and record the real breakdown under Comments.
2. Add `ComputeByRefererAndUri` to `AppSettings` and bind it; add `"ComputeByRefererAndUri": false` to `src/IisLogParserArcGIS/appsettings.json`.
3. Make `DailyAggregateBatch.ByRefererAndUri` nullable and teach `DailyAggregateReplacer` to skip a null table. Update the XML docs on both.
4. Gate the aggregation in `HarvestRunner.Execute`.
5. Log which state the run is in, so an empty table is explainable from the log file. Follow the ticket 03/05 logging pattern (`ILoggerFactory` optional with null fallback, ADR 0003) and keep to the existing template style. Implemented at **Warning**, not Information: the default `LogLevel` is `Warning`, so an Information line would never reach the log file in production, and the run summary lines are Warning for the same reason.
6. Tests:
   - `AppConfigurationFactoryTests`: default is `false`; `true` in JSON binds; the environment variable override works.
   - `DailyAggregateReplacerTests` (Data): a batch with a null `ByRefererAndUri` leaves an existing table's rows for that date untouched and still replaces every other table; a non-null (even empty) list still replaces the table, as today.
   - `HarvestRunnerTests`: with the setting `false`, a harvest leaves `aggregated_by_referer_and_uri` empty but populates the others; with `true`, it is populated as before (the existing tests probably rely on `true` and must set it explicitly). Check `RegressionTests` and Reports tests that read this table or launch the executable, and set the variable where they need it.
7. Docs: add the key to the README configuration table (default, meaning, and that with `false` the table exists but is empty, and that flipping it does not reclaim space already used); mention it wherever the README lists the aggregates or the `aggregated_by_referer_and_uri` table. Note in `requirements.md` at the aggregate's section that it is computed only when enabled. Add a line to `CONTEXT.md` only if it describes this aggregate as always present (it does not name it today).
8. Build with 0 warnings; Domain, Host, Data, Regression and Reports suites pass.

## Acceptance criteria

- [x] `ComputeByRefererAndUri` exists on `AppSettings`, defaults to `false`, and is overridable via `IISLOGPARSER_ComputeByRefererAndUri`.
- [x] With `false`, a harvest into a fresh database leaves `aggregated_by_referer_and_uri` present and empty, every other aggregate is unchanged, and the run does not build the referer+URI aggregate in memory.
- [x] With `true`, the output is identical to today's, row for row.
- [x] With `false`, rows already present in `aggregated_by_referer_and_uri` (from a run with `true`) are left untouched by a later harvest.
- [x] The setting is documented in the README configuration table, and the run log states when the aggregate is skipped.
- [x] Build with 0 warnings; all suites pass.

## Decisions (owner)

- **Rows already in the table are left alone when the setting is `false`.** Skipping the table entirely means flipping the setting off never deletes data computed earlier, and re-harvesting an old day cannot wipe that day's rows. The cost is that after the setting changes, the table can hold rows for some days and none for others; each stored day is still correct for that day, since a Daily Batch is derived from the same log files.
- **The default is `false`**, in the shipped `appsettings.json` and in the code. This changes what existing deployments write on their next harvest, so the README must say so.

## Out of scope

- **Reclaiming space in the existing 2023-2026 databases.** Stopping the writes only stops the growth. Removing what is already stored is a separate manual step, done per database on a copy after freeing space, because `VACUUM` needs roughly the database's size in free space: `DELETE FROM aggregated_by_referer_and_uri; VACUUM;` (or `DROP TABLE` then re-run `EnsureCreated`). A tool or verb for this is a separate ticket if wanted.
- **`aggregated_by_uri` and the other unbounded tables.** If the `dbstat` output in task 1 shows `aggregated_by_uri` is also large, capping it (for example keeping only the top N URIs per day) is a different trade-off: it changes what the top-500 URI Leaderboard can report over a range. Open a ticket only if the numbers justify it.
- **Rendering the Dashboard on demand.** Measured and rejected: the HTML is about 2% of the footprint, and it would reverse ADR 0005 for almost no saving.
- **Missing `local_date` indexes** (see the out-of-scope note in ticket 27): unchanged.

## Comments

Raised by the owner after running three years of logs on the server: 111 GB under `D:\inetpub\wwwroot` (51,101 files), of which 109.1 GB is the four SQLite files (per-year sizes above) and about 2.3 GB is the 2026 Dashboard (50,353 files, roughly 45 KB per page). The older years' folders hold only 249 files each. The finding that nothing in the Reports project reads `aggregated_by_referer_and_uri` comes from a search of the source; the claim that it dominates the database is **not yet measured**, hence task 1.

Implemented: `AppSettings.ComputeByRefererAndUri` (default `false`, shipped `appsettings.json` sets it explicitly); `DailyAggregateBatch.ByRefererAndUri` is nullable and `DailyAggregateReplacer` skips a null table; `HarvestRunner` only aggregates when the setting is on and otherwise calls the new `RunSummaryReporter.ReportByRefererAndUriSkipped` (Warning). Tests added in the Data (replacer), Host (configuration, harvest runner, reporter) suites; README configuration table, output-schema table and step 5, and `requirements.md` section 6 updated. Task 1 (the `dbstat` check) needs the server's database and is not done: until it is, "this table dominates the 109 GB" remains an inference from the code.
