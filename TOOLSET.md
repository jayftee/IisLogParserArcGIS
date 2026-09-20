# Toolset

This project's static-analysis and tooling baseline was applied by the `init-csharp-project` Claude
Code skill. This file exists because a `README.md` here would be the project's own — this one is
scoped purely to "what did the skill drop in, what does each thing depend on, and why." If a step
below doesn't make sense, or a rule fires and you don't know which of the four analyzer layers owns
it, start here before digging into any single file.

Nothing in this file is enforced by tooling — it's documentation only. If you change one of the files
below, update this file's description of it too, or a future reader (human or agent) will be working
from a stale picture.

## The dependency shape, in one paragraph

`Directory.Build.props` is the hub: MSBuild auto-imports it into every `.csproj` at or below this
folder, and it's the only file that actually references the other analyzer-settings files
(`stylecop.json`, `cleancoders.json`) and pulls in the analyzer packages themselves. `.editorconfig`
is independent of that — it's discovered by IDEs/analyzers via their own upward directory walk — but
its diagnostic-severity lines (`dotnet_diagnostic.CC0042.severity`, etc.) only mean anything once the
matching analyzer package is actually referenced, so the two files are coupled in practice even though
neither literally points at the other. `nuget.config`, if present, is what lets one of those packages
(`CleanCoders.Analyzers`) resolve at all.

## Files

### `.editorconfig`

Formatting, naming, and diagnostic-severity rules for every layer below — `CA` (OOB Roslyn), `SA`
(StyleCop), `CC` (CleanCoders.Analyzers). Discovered automatically by MSBuild, Roslyn, and every
mainstream IDE via an upward directory walk from any file being edited/built — no reference to it
exists anywhere else, and it doesn't need a `.csproj` to already exist to take effect once one shows
up beneath it.

Depends on nothing. Everything below depends on *it* being tuned correctly once their packages are
referenced — most severities here are inert no-ops until the matching analyzer is actually installed.

Known gaps carried over from setup, not yet resolved by real project use:
- `VSTHRD*` (`Microsoft.VisualStudio.Threading.Analyzers`) has zero severity overrides here — those
  rules fire at whatever the analyzer defaults to.
- `CA1416` (platform compatibility) and `CA1515` (types can be made internal) are suppressed via
  `Directory.Build.props`'s `NoWarn`, not here — bootstrap suppressions pending a real platform/API-
  surface decision for this specific project. Revisit and remove once that decision is made.
- `AnalysisMode=AllEnabledByDefault` (set in `Directory.Build.props`) means this project starts
  maximally noisy on `CA` rules by design — expect a first-build wave of warnings; suppress
  individually here as false positives surface, don't turn the mode back down.

### `Directory.Build.props`

Repo-root MSBuild properties, auto-imported into every project beneath this folder — no `<Import>`
needed anywhere. This is the file that actually wires the four analyzers together:

1. **OOB Roslyn/`NetAnalyzers` (`CA` rules)** — via `EnableNETAnalyzers`/`AnalysisLevel=latest`/
   `AnalysisMode=AllEnabledByDefault`. No package reference; built into the SDK.
2. **`Microsoft.VisualStudio.Threading.Analyzers`** (pinned version — check the `PackageReference` in
   this file for the exact one currently in use) — async/threading rules (`VSTHRDxxx`). Resolves from
   nuget.org normally.
3. **`StyleCop.Analyzers`** (pinned version — see the `PackageReference`) + `AdditionalFiles` link to
   `stylecop.json` next to this file. Resolves from nuget.org normally.
4. **`CleanCoders.Analyzers`** (pinned version — see the `PackageReference`) + `AdditionalFiles` link
   to `cleancoders.json`. **Does not resolve from nuget.org** — needs the local feed at
   `C:\_LocalNuGet` on the machine building this project, either registered machine-wide or via this
   project's own `nuget.config` (see below). If `dotnet restore` fails specifically on this package,
   that feed is the first thing to check.

Also sets `Nullable`, `ImplicitUsings`, `LangVersion=latest`, `EnforceCodeStyleInBuild` (without this,
`.editorconfig`'s `IDE0xxx` rules only show as IDE squiggles and never fail `dotnet build`), and
`GenerateDocumentationFile=true` (every public member needs a doc comment, repo-wide, unless a project
opts out locally). A handful of bootstrap `NoWarn`s live here too (`SA0001`, `SA1633`/`SA1636` file
headers, `SA1516`, `SA1101`, `SA1309`, `CA1416`, `CA1515`) — each has a comment explaining why in the
file itself; don't remove one without reading its comment first.

Depends on: nothing (it's the root of the dependency graph below it). Everything else's wiring flows
*from* this file.

### `stylecop.json`

`StyleCop.Analyzers`' own settings — using-directive placement (outside namespace), no Hungarian
prefixes, member ordering, file-header format. Referenced by `Directory.Build.props`'s
`AdditionalFiles` entry; without that reference, `StyleCop.Analyzers` would fall back to its classic
defaults (e.g. usings *inside* the namespace) and silently ignore this file.

Known gap: `documentationRules.companyName` ships as `"Acme"` — a placeholder, currently harmless
because `Directory.Build.props` suppresses the file-header rules (`SA1633`/`SA1636`) that would
otherwise consume it. Set a real value here (or confirm the placeholder is fine) before those rules
ever get turned back on.

### `cleancoders.json`

`CleanCoders.Analyzers`' rule parameters (max method lines, max method arguments, max class lines,
etc. — see the file itself for the full current set). Referenced the same way as `stylecop.json`, via
`Directory.Build.props`'s `AdditionalFiles`. Validated against the analyzer's own embedded schema at
build time (`CleanCoders.Analyzers` 0.2.1+) — a `CCCFG001`/`CCCFG002`/`CCCFG003` diagnostic means this
file has a value below the schema's minimum, an unrecognized property, or isn't valid JSON,
respectively.

Only a subset of `CleanCoders.Analyzers`' rules are actually enabled at any given severity — that's
controlled in `.editorconfig` (`dotnet_diagnostic.CC####.severity`), not here. This file only supplies
*parameters* for whichever rules are turned on there.

### `.gitignore`

Standard .NET ignores — `bin/`/`obj/`/`out/`, IDE junk (`.vs/`, `.idea/`, `.vscode/` except
`extensions.json`), user-specific files (`*.user`, `*.suo`), NuGet packages, test results, and a few
"never commit these" patterns (`*.pfx`, `*.snk`, `secrets.json`, `.env`, `appsettings.*.local.json`).
If this project already had a `.gitignore` before the skill ran, only the missing lines were appended
under a `# from init-csharp-project` header — check there first if something looks duplicated.

Depends on nothing; nothing depends on it besides `git` itself.

### `nuget.config`

Adds `C:\_LocalNuGet` as a package source under the key `LocalCleanCoders`, specifically so
`CleanCoders.Analyzers` (referenced in `Directory.Build.props`, see above) can restore — it's not
published to nuget.org. `Microsoft.VisualStudio.Threading.Analyzers` and `StyleCop.Analyzers` don't
need this; they resolve from nuget.org as normal regardless of whether this file exists. Present here
because `C:\_LocalNuGet` was not already resolvable as a machine-wide source when this skill ran
(checked via `dotnet nuget list source`).

### `.config/dotnet-tools.json`

The standard .NET local-tool manifest — records which CLI tools (and exact versions) this project
expects, restorable via `dotnet tool restore` on any machine/CI runner without a global install.
Currently lists `Crap4DotNet` (see below), pinned at whatever version is recorded in the manifest —
check there for the exact version rather than assuming it matches what's written in this doc.

### `Crap4DotNet` (a local tool, not a static file)

A CRAP-score reporter — `complexity(m)² × (1 − coverage(m))³ + complexity(m)` — flagging methods that
are both complex *and* poorly tested. **Not part of the four analyzers above and not wired into
`Directory.Build.props`**: it plays no role in `dotnet build` at all. It's a CLI tool, run on demand:

```
dotnet tool restore
dotnet dotnet-crap analyze --run-tests --threshold 30 --output crap-report.json
```

(`--run-tests` has it execute the test suite itself to produce coverage; alternatively point it at an
existing Cobertura XML file, e.g. from `coverlet`. A `diff` subcommand compares two reports, useful
before/after a refactor.)

Use `dotnet dotnet-crap ...`, not `dotnet tool run dotnet-crap ...` — confirmed that the latter has
`dotnet tool run` itself swallow a trailing `--help` and print its own generic help instead of
forwarding it to the tool. `dotnet dotnet-crap` is the form the tool's own install message recommends
and the one confirmed to work.

It has **no project-level config file** — everything above is CLI flags, re-specified on every
invocation. If a fixed threshold or standard invocation needs to be remembered project-to-project,
that has to live in a wrapper script, a `Makefile`/task-runner target, or a documented command
somewhere in this project — the tool itself has nowhere to persist it.

### `docs/`, `src/`, `tests/`

`docs/` already held this project's `agents/` and `adr/` subfolders before this skill ran. `src/` and
`tests/` were created empty by this skill (each holding a `.gitkeep` so they survive the first commit)
— per this project's own design (see `docs/adr/0004-solution-layout-mirrored-tests.md`), `src/` will
hold one project per layer (the console host, `.Domain`, `.Data`) and `tests/` one mirrored xUnit
project per `src/` project plus a separate, non-mirrored regression-test project.
