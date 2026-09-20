# 03 — File discovery

**What to build:** Given the three CLI arguments, the program correctly identifies and lists every physical log file needed to fully cover the requested local day, and fails loudly when none exist.

**Blocked by:** 02

**Status:** done

- [x] A pure Domain function computes which UTC calendar date(s) are needed for a given local date and configured time-zone offset (the target date, plus the UTC-adjacent date on whichever side the offset's sign requires; none when the offset is zero)
- [x] The Host globs the configured source directory for `u_ex<YYMMDD>_x_*.log` for each needed UTC date, ignoring the trailing site-ID/server segment entirely
- [x] Running against the real log directory for a known local date selects the correct file(s), including the UTC-adjacent file where applicable
- [x] Running against a directory with no matching files exits non-zero with a clear "no files found" message, before any parsing is attempted
- [x] The Domain date-computation function is unit tested for both offset signs and the zero-offset case

## Comments

Implemented as a Domain/Host split, per spec.

- **Domain** (`IisLogParserArcGIS.Domain/FileDiscovery/RequiredUtcDatesCalculator.cs`): pure static `Calculate(DateOnly targetLocalDate, TimeSpan localUtcOffset)`, zero I/O. Negative offset → `[target, target+1]`; positive → `[target-1, target]`; zero → `[target]`. Unit tested for both signs, zero, and year-boundary rollover in both directions.
- **Host** (`IisLogParserArcGIS/FileDiscovery/`): `LocalUtcOffsetResolver.Resolve(timeZoneId, localDate)` turns the configured `AppSettings.LocalTimeZone` into the `TimeSpan` the Domain function needs, via `TimeZoneInfo.FindSystemTimeZoneById` + `GetUtcOffset` at local midnight (throws `TimeZoneNotFoundException`/`InvalidTimeZoneException` for a bad config value — now reported by `Program.cs` as its own "Invalid configured local time zone" message rather than being lumped in with config/logging failures). `LogFileLocator.Discover(sourceDirectory, targetLocalDate, localUtcOffset)` calls the Domain function, then globs `u_ex{yyMMdd}_x_*.log` per needed UTC date, filtering out any 8.3-short-name false match on the `.log` extension.
- **Fail loudly, and don't silently drop partial days**: every required UTC date must have at least one matching file — if *any* one of them (not just the total) comes back empty, `LogFileLocator` throws `NoLogFilesFoundException` (caught in `Program.cs`, written to `Console.Error`, exit code 1), logged at Error first so it's visible in the rolling log file too. This came out of code review: an earlier version only failed when the *combined* count across all needed dates was zero, which would have silently returned a half-covered day (missing exactly the UTC-adjacent file the whole offset logic exists to fetch) as a "successful" run whenever that file hadn't arrived yet — directly contradicting the spec's "hits near local midnight are never silently dropped" story.
- `IisLogParserArcGIS.Domain.Tests` and `IisLogParserArcGIS.Tests` both had their first real content added; both csproj files were missing the `GenerateDocumentationFile=false` test-project escape hatch already present in `IisLogParserArcGIS.Tests.csproj` (only surfaced now that they have classes to document) — added to both for consistency, including `Data.Tests` pre-emptively.
- Verified manually against the real `2026/` corpus (`DOTNET_ENVIRONMENT=Development` for Debug-level logging): zero-offset (`UTC`) selects only the single matching UTC-dated file(s); temporarily overriding `LocalTimeZone` to `America/New_York` (negative offset) for `2026-05-01` correctly pulls in the `2026-05-02`-dated file as well; a date with no corpus coverage (`2050-01-01`) exits 1 with a clear message on both console and the rolling log file, before any parsing. `appsettings.Development.json` was reverted to its committed content afterward.
- `dotnet build` on the full solution: 0 warnings, 0 errors. `dotnet test`: 45 passed (5 Domain, 40 Host) across the two mirrored projects with content so far.
