---
status: accepted
---

# Explicit `harvest`/`regenerate`/`harvest-regenerate` verbs, no default verb

The CLI used to accept exactly one shape of invocation — three positional arguments that always harvested a Local Date's logs *and* regenerated the Dashboard. That was wasteful for a backfill: `Invoke-IisLogBackfill.ps1` calls the executable once per date in a backlog, so a full year's worth of dates triggered a full Dashboard rebuild after every single date, when only the very last one's output matters.

We considered adding two boolean flags (`--skip-dashboard`, `--dashboard-only`) to the existing single command instead of introducing verbs. Flags would have kept today's bare invocation working unchanged, but `--dashboard-only` would still have forced the parser to require (or the caller to fake) a `logSourceDirectory`/`targetLocalDate` it never uses, since `CommandLineParser` ties positional values to one fixed argument shape regardless of which flags are set.

Instead we split the CLI into three explicit `CommandLineParser` verbs — `harvest`, `regenerate`, and `harvest-regenerate` — each with exactly the argument list it uses, and dropped the old bare invocation entirely: every caller now states its verb, with no implicit default. This is a deliberate compatibility break, justified by this being an internal tool with effectively one real caller (`Invoke-IisLogBackfill.ps1`) plus occasional manual runs — the cost of updating that one caller and this repo's own docs was judged lower than the ongoing cost of a flag-based interface where `regenerate` would otherwise have to accept meaningless arguments.
