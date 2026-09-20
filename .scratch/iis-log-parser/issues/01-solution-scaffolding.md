# 01 — Solution scaffolding

**What to build:** The repository has a working, buildable .NET solution with the correct project layout and analyzer baseline applied, so every subsequent ticket has somewhere to add code without first inventing structure.

**Blocked by:** None — can start immediately

**Status:** done

- [x] Solution contains three `src/` projects: the console host (root namespace `IisLogParserArcGIS`), `IisLogParserArcGIS.Domain`, and `IisLogParserArcGIS.Data`
- [x] Solution contains four mirrored `tests/` projects: `IisLogParserArcGIS.Tests`, `IisLogParserArcGIS.Domain.Tests`, `IisLogParserArcGIS.Data.Tests`, plus `IisLogParserArcGIS.RegressionTests` (intentionally outside the 1:1 mirror, per ADR 0004)
- [x] The `init-csharp-project` analyzer baseline (NetAnalyzers, VS Threading Analyzers, StyleCop, CleanCoders.Analyzers) applies cleanly to every project with zero warnings on the empty scaffold
- [x] `appsettings.json` exists with placeholders for local time zone, log output directory, and application log level, with an `appsettings.Development.json` overlay
- [x] The solution builds and the console host runs as a no-op via `dotnet run`

## Comments

Scaffolded via `dotnet new`: `IisLogParserArcGIS.slnx` at the repo root, three `src/` projects (console host, `.Domain`, `.Data`) and four `tests/` xUnit projects (net10.0, matching the installed SDK). Project references: `Data` → `Domain`; console host → `Domain` + `Data`; `Tests`/`RegressionTests` → console host (per ADR 0004, exercising all layers); `Domain.Tests` → `Domain`; `Data.Tests` → `Data`. Template-generated `Class1.cs`/`UnitTest1.cs` files were deleted so every project is genuinely empty (StyleCop's mandatory doc-comment rules would otherwise fire on the generated public types). Console host's `Program.cs` is a single `return;` top-level statement. `appsettings.json`/`appsettings.Development.json` hold placeholder keys (`LocalTimeZone`, `LogOutputDirectory`, `LogLevel`) and copy to the output directory — issue 02 does the actual binding/CLI/logging wiring. Verified: `dotnet build` (0 warnings, 0 errors) from a clean `bin`/`obj`, `dotnet run` on the console host (exit 0, no output), `dotnet test` (exit 0, 0 tests — expected for an empty scaffold), and `CleanCoders.Analyzers` restoring from the local feed.
