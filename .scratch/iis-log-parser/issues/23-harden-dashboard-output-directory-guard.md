# 23 — Harden the Dashboard output-directory guard against ancestors and other precious directories

**What to build:** A Regeneration Run deletes its output directory recursively (`RegenerationRun.Run`). After ticket 22 the only protections were "not a drive root" and "not exactly the executable's own directory". Anything else that *contains* something precious was still accepted. With `OutputDirectory` set to `".."` (or any ancestor of the executable's directory), to the directory holding the aggregate database, to the log output directory, or - worst - to the IIS log source directory of a `harvest-regenerate` run, the run wiped it. Refuse all of those before anything is deleted.

**Blocked by:** 22 (the guard lives in `DashboardRegenerator`, extracted there).

**Status:** done

## Rule

`DashboardRegenerator.Regenerate(connection, configuration, logSourceDirectory)` refuses the run (exit `1`, `Failed to regenerate the Dashboard: Refusing to use '<dir>' as the Dashboard output directory: it …`) when the resolved output directory **equals or contains**:

| Protected path | Where it comes from |
|---|---|
| the executable's own directory | `RunEnvironment.BaseDirectory` |
| the aggregate database file | `connection.DataSource` |
| the log output directory | bound `LogOutputDirectory`, resolved against the base directory |
| the log source directory | only for `harvest-regenerate`: `HarvestRunner` passes `parsedArguments.LogSourceDirectory`; `regenerate` has none |

Message shape: equal -> `it resolves to <description>, which this run would delete recursively.` (the pre-existing text for the executable's directory is unchanged); ancestor -> `it contains <description> '<path>', which this run would delete recursively.` Paths are compared after `Path.GetFullPath` and trimming trailing separators, ordinal-ignore-case, on directory boundaries (so `base-dashboard` is **not** mistaken for `base`). An output directory *inside* a protected directory (the default `Dashboard` next to the executable) is still allowed.

## Acceptance criteria

- [x] Output `".."`, `"../"`, the workspace root and its upper-cased form (all ancestors of the base directory) are refused; the base directory, database and log source file all survive.
- [x] Output equal to / containing the database's directory is refused and the database survives.
- [x] Output equal to the log output directory (`Logs`, `./Logs/`, `Logs/nested/..`) is refused and its log file survives; an output directory containing a configured log output directory elsewhere is refused.
- [x] Output equal to / containing the log source directory is refused for a harvest run and the IIS log file survives (`DashboardRegeneratorTests` and the `harvest-regenerate` wiring test in `HarvestRunnerTests`).
- [x] A sibling that only shares a name prefix with the base directory (`../base-dashboard`) is allowed and generated; an output directory inside a protected directory is allowed.
- [x] The drive-root case is still refused (now by the ancestor rule, message updated); `RegenerationRun`'s own drive-root guard stays as defense in depth.
- [x] Build with 0 warnings; Host, Domain, Data and Regression suites pass.

## Out of scope

- A marker-file scheme ("only delete a directory this program created"): a stronger guard, but a bigger behavior change.
- Protecting the *input* of a `regenerate` beyond the database and log directories.
- Symbolic links, junctions and 8.3 short names: the guard compares textual full paths, so a link that resolves into a protected directory is not detected. Comparison is ordinal-ignore-case (Windows semantics; this tool runs against IIS), which on a case-sensitive file system can only over-refuse.

## Comments

Follow-up to ticket 22, which found and fixed the trailing-separator hole in the equality check. Tests first: 14 new `DashboardRegeneratorTests` cases plus one `HarvestRunnerTests` case (Host suite 93 -> 109 together with ticket 24's one new test). `DashboardRegenerator.Regenerate` gained an optional `logSourceDirectory` parameter rather than a generic list, so each refusal names what it is protecting. In the CLI path a drive root is now reported as "it contains the executable's own directory" instead of the Reports project's "drive root" message; still refused, still exit 1. The check runs before `RegenerationRun.Run`, so nothing has been deleted when it fires.

Review notes. A `high` code review of this change raised four points: (1) `Path.GetFullPath` might throw `NotSupportedException`, which the filter does not list - not reproducible on .NET 10 (a colon-laden path such as `C:log:dir` does not throw; `.NET Core` dropped that check), while the reachable failure, `ArgumentException` from an embedded NUL in `LogOutputDirectory`, is already in the filter and now has a test (`regenerate` never looked at `LogOutputDirectory` before this ticket, so this is a new failure path worth pinning); (2) for `harvest-regenerate` the guard runs after the harvest has persisted, so a refused run leaves the day's rows written - kept on purpose, matching how every other regeneration failure behaves (existing test `..._ReturnsOneButKeepsTheHarvestedRows`) and safer than losing the day's data; (3) symlinks/junctions/short names - recorded above as out of scope; (4) the read-only-database test in ticket 24 - it did fail against the old message before the change, so it does detect the regression it was written for.
