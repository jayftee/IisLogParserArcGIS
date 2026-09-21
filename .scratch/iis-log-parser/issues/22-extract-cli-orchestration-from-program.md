# 22 — Extract the CLI's orchestration out of `Program.cs` into testable runner classes

**What to build:** `Program.cs` is currently the only untested code of any size in the repo: **112 of the 123 uncovered lines** in the latest coverage run (line coverage 95.6%; `Program` is 0% in every raw coverage file). The regression suite (ticket 15) does run the built executable, but out-of-process, so coverage never sees it and none of the logic below is unit-tested. `Program.cs` is meant to be a thin composition root (ticket 02 said so explicitly), but it has grown into the place where four kinds of logic live:

1. **Argument parsing and verb dispatch** (lines 18–25) — the only part that *should* stay in `Program.cs`.
2. **Harvest orchestration** (`RunHarvest`) — validate, read configuration, build logging, open the database, resolve the UTC offset, discover → parse → aggregate ten kinds → replace → report, and optionally regenerate.
3. **Regenerate orchestration** (`RunRegenerate` and `RegenerateDashboard`) — validate, read configuration, open the database, regenerate the Dashboard.
4. **Safety and error policy** — `ResolveReportsOutputDirectory` (refuses to let a recursive Dashboard delete run against the executable's own directory — **the highest-risk untested code in the repo**) and three separate `catch` filters that decide which exceptions become exit code `1` and with what message.

The goal is to move (2)–(4) into classes in the host project that can be unit-tested against real temp directories and a real SQLite file, leaving `Program.cs` as parse + dispatch only. **This ticket is behavior-preserving**: exit codes, stderr messages, side-effect ordering and log output stay exactly as they are today. The regression suite and the new unit tests together are the proof.

**Blocked by:** none — builds on ticket 19 (verbs, done) and ticket 02 (CLI/config/logging, done).

**Status:** done

## Current behavior contract (must be preserved and pinned by tests)

Read `src/IisLogParserArcGIS/Program.cs` first. The behavior the new tests must lock in, before any code moves:

| Situation | Exit code | stderr |
|---|---|---|
| `harvest` / `harvest-regenerate` with a malformed argument (`CliArgumentValidationException`) | `1` | the exception message, nothing else |
| `regenerate` with a malformed argument, or a database path that does not exist | `1` | the exception message |
| `harvest*` with an invalid configured `LocalTimeZone` (`TimeZoneNotFoundException` / `InvalidTimeZoneException`) | `1` | `Invalid configured local time zone '<id>': <message>` |
| `harvest*` finds no log files (`NoLogFilesFoundException`) | `1` | the exception message |
| `harvest*` hits `FileNotFoundException` / `FormatException` / `IOException` / `UnauthorizedAccessException` / `SqliteException` | `1` | `Failed to initialize configuration, logging, or the output database: <message>` |
| `regenerate` hits the same set of exceptions | `1` | `Failed to initialize configuration or the input database: <message>` |
| Dashboard regeneration hits any of those, plus `TimeZoneNotFoundException` / `InvalidTimeZoneException` / `ArgumentException` | `1` | `Failed to regenerate the Dashboard: <message>` |
| `harvest` succeeds | `0` | — (and `RegenerationRun.Run` is **not** invoked, so no Dashboard directory appears) |
| `harvest-regenerate` succeeds | `0` | — (Dashboard is generated) |
| `regenerate` succeeds | `0` | — (no log discovery/parsing/aggregation happens) |
| Configured Dashboard output directory resolves to the executable's own directory | `1` | `Failed to regenerate the Dashboard: Refusing to use '<path>' as the Dashboard output directory: it resolves to the executable's own directory, which this run would delete recursively.` |

Other ordering facts worth keeping: `DOTNET_ENVIRONMENT` defaults to `Production` when unset; the database is opened and `AggregateDatabaseSchema.EnsureCreated` is called *before* the time zone is resolved; the stopwatch starts after that and stops after `DailyAggregateReplacer.Replace`; the run summary and both attribution summaries are reported after the stopwatch stops; regeneration (for `harvest-regenerate`) happens last, on the same open connection.

## Design

- **`RunEnvironment`** (sealed record, `Cli/`): `BaseDirectory`, `EnvironmentName`, `Error` (`TextWriter`), `TimeProvider`. Everything the orchestration currently reaches for statically (`AppContext.BaseDirectory`, `Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")`, `Console.Error`, `TimeProvider.System`) becomes an injected value so tests can point at a temp directory and capture stderr. One parameter object rather than four constructor parameters, per the repo's CC0042 analyzer.
- **`DashboardRegenerator`** (`Cli/`): the shared tail of both verbs — bind `ReportsSettings`, resolve and guard the output directory, call `RegenerationRun.Run`, map exceptions to the exit code and the `Failed to regenerate the Dashboard:` message. Owns `ResolveReportsOutputDirectory`. Used by both runners; this removes the current `RegenerateDashboard` duplication.
- **`RegenerateRunner`** (`Cli/`): takes `RegenerateArguments`, returns an `int` exit code. Validation, configuration, open database, `EnsureCreated`, delegate to `DashboardRegenerator`.
- **`HarvestRunner`** (`Cli/`): takes `IHarvestArguments` plus `alsoRegenerate`, returns an `int`. The whole current `RunHarvest` body, unchanged in order and behavior. Do **not** split the aggregation-batch construction into a further class — it is 15 lines of straight-line code and an extra class there adds indirection without a testability gain.
- **`Program.cs`** ends up as: build a `RunEnvironment` from the real process, parse, `MapResult` into the two runners, return the code. Nothing else. The ~8 lines that remain will stay visibly uncovered; do not add `[ExcludeFromCodeCoverage]` — the regression suite already exercises that shell end to end.
- Tests follow ADR 0004's 1:1 mirroring: `tests/IisLogParserArcGIS.Tests/Cli/{DashboardRegeneratorTests,RegenerateRunnerTests,HarvestRunnerTests}.cs`. They use real temp directories, a small hand-written `appsettings.json` in the temp base directory, tiny log files, and a real SQLite file — **no mocks** for the static collaborators (`SqliteConnectionFactory`, `LogFileLocator`, `DailyAggregateReplacer`, …); they are cheap and real ones are what prove the contract. Existing tests such as `SerilogLoggerFactoryBuilderTests` (temp subdirectory, cleaned up in `finally`) show the house style.
- The temp `appsettings.json` needs the keys `AppConfigurationFactory` and `ReportsConfigurationFactory` bind (`LocalTimeZone`, `LogOutputDirectory`, `LogLevel`, `PortalWebAdaptorName`, `OutputDirectory`, and, for a regeneration that produces pages, the roots/base-URL keys). Look at `src/IisLogParserArcGIS/appsettings.json` for the full shape; blank values fall back to documented defaults, so a minimal file is fine.

## Tasks

Do these in order. Each task leaves the solution building with 0 warnings and every suite green, so it can be committed on its own.

### Task 1 — Introduce `RunEnvironment` and extract `DashboardRegenerator` (with the deletion-guard tests)

The smallest, highest-value slice first: it puts the riskiest untested code under test and creates the shared piece both runners need.

- Add `RunEnvironment` and `DashboardRegenerator`. Move `RegenerateDashboard` and `ResolveReportsOutputDirectory` out of `Program.cs` into `DashboardRegenerator`; `Program.cs` calls it (both existing local functions delegate) so the program still works after this task.
- `DashboardRegeneratorTests`:
  - a configured `OutputDirectory` that resolves to the base directory itself (`"."`, the absolute path, a trailing-slash variant, a different-case variant on Windows) returns `1`, writes the exact `Refusing to use …` message, and **leaves the base directory and its contents untouched** (put a sentinel file in it and assert it survives);
  - a relative `OutputDirectory` is resolved against the base directory, and the Dashboard is generated there on success (`0`, `index.html` or another known page exists);
  - each exception class in the `Failed to regenerate the Dashboard:` filter maps to `1` and the documented message. Reachable cheaply: an invalid `LocalTimeZone` in the settings (`TimeZoneNotFoundException`), a drive-root output directory (`ArgumentException` from `RegenerationRun`), an output directory whose parent is a file (`IOException`); if one of the others has no cheap trigger, say so in the ticket comments rather than forcing it.

### Task 2 — Extract `RegenerateRunner`

- Move `RunRegenerate` into `RegenerateRunner` (validate → build configuration → open database → `EnsureCreated` → `DashboardRegenerator`), taking `RunEnvironment` and `RegenerateArguments`. `Program.cs` dispatches to it.
- `RegenerateRunnerTests`:
  - a database path that does not exist returns `1` with the validator's "does not exist … never creates one" message and **does not create the file**;
  - an empty path returns `1` with the validator message;
  - a valid, previously harvested database (create one with `SqliteConnectionFactory` + `AggregateDatabaseSchema.EnsureCreated`, optionally with rows via the repositories) returns `0` and produces the Dashboard directory;
  - a missing `appsettings.json` (`FileNotFoundException`) returns `1` with `Failed to initialize configuration or the input database: …`;
  - a corrupt database file (`SqliteException`) returns `1` with the same prefix.

### Task 3 — Extract `HarvestRunner`

- Move `RunHarvest` into `HarvestRunner`, taking `RunEnvironment`, `IHarvestArguments` and `alsoRegenerate`. Keep the body's order exactly as the behavior contract above lists it, including the odd-but-existing facts (database opened before the time zone is resolved). `Program.cs` dispatches both `HarvestArguments` and `HarvestRegenerateArguments` to it.
- `HarvestRunnerTests` (build each fixture in a temp directory; one tiny `u_ex<yyMMdd>.log` file with a handful of lines is enough — reuse the fixture-building approach in `LogFileLocatorTests` / `LogCorpusParserTests`):
  - happy path `harvest`: returns `0`, the database contains the expected aggregate rows for the target date (query through the repositories), **no Dashboard directory is created**;
  - happy path `harvest-regenerate` (`alsoRegenerate: true`): returns `0`, rows are persisted *and* the Dashboard directory exists;
  - re-running the same date is idempotent (the replacer's contract, but proven end to end through the runner once);
  - malformed date, empty log source directory argument → `1` with the validator message, database file not created;
  - log source directory with no matching files → `1` with the `NoLogFilesFoundException` message;
  - invalid configured `LocalTimeZone` → `1` with `Invalid configured local time zone '<id>': …`;
  - unwritable/invalid output database path (`IOException` / `SqliteException`) → `1` with `Failed to initialize configuration, logging, or the output database: …`;
  - missing `appsettings.json` → `1` with the same prefix;
  - `EnvironmentName` is honored: with a `Development` overlay file that changes a value the test can observe (e.g. a different `LogOutputDirectory`), the overlay wins; with `Production` it does not.
  - the run's log file is written under the configured `LogOutputDirectory` inside the temp base directory (proves `SerilogLoggerFactoryBuilder` receives the injected base directory, and that the logger factory is disposed so the temp directory can be deleted afterwards).

### Task 4 — Reduce `Program.cs` to parse + dispatch and prove nothing changed

- Delete the now-empty local functions; `Program.cs` builds one `RunEnvironment` (`AppContext.BaseDirectory`, `DOTNET_ENVIRONMENT` or `"Production"`, `Console.Error`, `TimeProvider.System`) and dispatches the three verbs. Remove `using`s that are no longer needed.
- Run the full solution: build with 0 warnings, then every suite (Domain, Host, Data, Reports, Regression). The regression suite is the black-box proof that exit codes, messages and outputs did not move; if it needs to change, that is a behavior change and this ticket has gone wrong.
- Manual smoke test of the built executable for the three verbs, one failure path each (`harvest` with a bad date, `regenerate` with a missing database, `harvest-regenerate` against a log directory with no files), comparing stderr text to the table above.

### Task 5 — Re-measure coverage and close out

- Re-run the coverage report and record the new numbers in the Comments: overall line coverage, `Program` (expect it to drop to the parse/dispatch shell only), and the three new classes (expect ≥ 95% line and branch each; explain any residual uncovered line).
- Tick the acceptance criteria, set `Status: done` and write the Comments as the earlier tickets do (what shipped, decisions made, anything surprising) **before** committing, not afterwards.

## Acceptance criteria

- [x] `Program.cs` contains only argument parsing, verb dispatch and construction of the `RunEnvironment`; no orchestration, no `catch` filters, no path logic.
- [x] `RunEnvironment`, `DashboardRegenerator`, `RegenerateRunner` and `HarvestRunner` exist in the host project's `Cli/` namespace with their mirrored test classes under `tests/IisLogParserArcGIS.Tests/Cli/`.
- [x] Every row of the behavior-contract table is asserted by at least one test (exit code **and** stderr text where stated).
- [x] The executable-directory deletion guard is covered, including proof that the base directory's contents survive a refused run.
- [x] `RunEnvironment` is the only way the runners obtain the base directory, environment name, error writer and clock; no static `Console`, `AppContext` or `Environment` access remains in the runners.
- [x] No behavior change: the regression suite passes unmodified.
- [x] The solution builds with 0 warnings; Domain, Host, Data, Reports and Regression suites all pass.
- [x] Coverage re-measured; `Program` reduced to the parse/dispatch shell and the three new classes ≥ 95% line and branch coverage, numbers recorded in the Comments.
- [x] No test uses mocks for the static collaborators; temp directories are cleaned up (no leaked log-file locks or pooled SQLite connections — clear only that database's pool, as ticket 28's fix does).

## Out of scope

- **Changing any behavior.** Two things noticed while reading `Program.cs` are deliberately *not* fixed here and should become their own tickets once the runners exist and make them testable: (a) an invalid `LocalTimeZone` is only detected *after* the output database has been opened and its schema created, so a bad configuration still creates or touches the output database; (b) the `IOException` catch in the harvest path says "Failed to initialize configuration, logging, or the output database", but the same filter also catches I/O failures while reading log files mid-parse, so the message can mislead.
- Splitting the aggregation-batch construction into its own class.
- The unused analyzer-mandated constructors on `CliArgumentValidationException` and `NoLogFilesFoundException` (about 9 uncovered lines) and the single uncovered branch in `PageShellRenderer` — unrelated to this refactor.
- Any change to verbs, argument shapes, `appsettings.json` keys or the regression suite.

## Comments

Implemented. `Program.cs` is now parse + dispatch only (build one `RunEnvironment`, parse, `MapResult` into the runners); all orchestration lives in `src/IisLogParserArcGIS/Cli/`: `RunEnvironment` (sealed record: base directory, environment name, error `TextWriter`, `TimeProvider`), `DashboardRegenerator` (Reports settings binding, output-directory guard, `RegenerationRun.Run`, the `Failed to regenerate the Dashboard:` mapping), `RegenerateRunner` (`Run(RegenerateArguments)`) and `HarvestRunner` (`Harvest(args)` / `HarvestAndRegenerate(args)`). Mirrored tests: `DashboardRegeneratorTests` (11), `RegenerateRunnerTests` (7), `HarvestRunnerTests` (13), plus one shared `TestSupport/CliTestWorkspace` (temp base/`logs-in`/`data` tree that clears only its own databases' SQLite pools). Real temp directories, real SQLite, real `LogFileLocator`/`DailyAggregateReplacer`; no mocks.

**A real bug in the deletion guard, found by the tests and fixed here (the one deliberate behavior change).** `ResolveReportsOutputDirectory` compared `Path.GetFullPath(resolved)` to `Path.GetFullPath(baseDirectory)` as strings. `AppContext.BaseDirectory` always ends in a directory separator, but a resolved `"."` (or `"sub/.."`, or the base directory written without its trailing slash) does not, so the two never compared equal and the guard did **not** fire: with `OutputDirectory` set to `"."` the Regeneration Run went on to delete the executable's own directory recursively. The tests reproduced exactly that (3 of the 6 theory cases failed against the moved-verbatim code; the base directory really was deleted). The fix compares both paths after `Path.TrimEndingDirectorySeparator`. The behavior-contract table already promised the guard fires in this situation, so this restores the documented behavior rather than changing it, but it is a change from what the shipped exe did, so it is flagged here. The regression suite never exercised it (it only runs `harvest`).

**Shape decisions that differ slightly from the ticket text.**
- `HarvestRunner` exposes two methods, `Harvest` and `HarvestAndRegenerate`, instead of `Run(args, bool alsoRegenerate)`: the repo's CC0041 analyzer rejects boolean parameters. A private `PostHarvestStep` enum carries the choice into the single private `Execute`.
- `Execute` is ~73 lines; CC0034 (max 50) is suppressed with a justification, following the precedent in `ArcGisServerSectionBuilder`. Splitting it needs parameter objects to stay under CC0042's 3-argument limit, which is the extra class the ticket said not to add. The body is otherwise moved verbatim, order unchanged (including database opened and schema created before the time zone is resolved).
- `RunEnvironment` itself has four components, which CC0042 flags; suppressed with a justification (a parameter object is the whole point).
- Dashboard-failure tests cover `ArgumentException` (guard, drive root), `TimeZoneNotFoundException` (invalid zone) and `IOException` (output parent is a file). `FileNotFoundException`, `FormatException`, `UnauthorizedAccessException`, `InvalidTimeZoneException` and `SqliteException` inside the Dashboard step have no cheap trigger and are not individually tested; they share the same one-line filter and message.
- README updated: the sequence diagram participant, the "fail loudly" pointer and the "where to find things" table now point at the runners and `Program.cs` as the verb dispatcher.

**Verification.** Solution builds with 0 warnings, 0 errors. Domain 250, Host 93 (was 62), Data 192, Regression 4, Reports 181, all passing; the regression project is unmodified. Manual smoke test of the built exe: `harvest` with a bad date, `regenerate` with a missing database and `harvest-regenerate` with an empty log directory each exit `1` with exactly the stderr text from the table; a bare invocation still prints usage and exits `1`.

**Coverage (tools/Invoke-CodeCoverage.ps1, 2026-09-20).** Overall line coverage 95.6% -> 99.1% (uncovered lines 123 -> 23); branch 93.5% -> 95.8%; host assembly 64.7% -> 94.3%. `DashboardRegenerator`, `HarvestRunner` and `RegenerateRunner`: 100% line and 100% branch each. `Program`: still 0%, now only the ~8-line parse/dispatch shell, deliberately left visible. The remaining uncovered lines are that shell, the unused analyzer-mandated exception constructors, and the one `PageShellRenderer` branch (all out of scope). `RunEnvironment` is a record with only auto-properties and no coverable lines.

**Follow-ups, still not fixed here:** (a) the invalid-time-zone check happens after the output database is created (smoke test confirms `b.sqlite` is left behind even when no logs are found); (b) the `IOException` message is misleading for mid-parse I/O failures.
