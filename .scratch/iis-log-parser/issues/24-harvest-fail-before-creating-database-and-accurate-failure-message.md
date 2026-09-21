# 24 — Harvest: reject an invalid time zone before creating the database, and report post-initialization failures accurately

**What to build:** Two behavior issues noticed while extracting the runners (ticket 22), both in `HarvestRunner`.

1. An invalid configured `LocalTimeZone` was only detected *after* `SqliteConnectionFactory.Open` and `AggregateDatabaseSchema.EnsureCreated`, so a bad configuration still created (or touched) the output database file.
2. One `catch` reported every `IOException` / `SqliteException` / `UnauthorizedAccessException` / `FileNotFoundException` / `FormatException` as `Failed to initialize configuration, logging, or the output database: …`, including failures that happen later while the logs are being harvested (e.g. a write failure during the atomic replace), which is misleading.

**Blocked by:** 22

**Status:** done

## Acceptance criteria

- [x] The configured time zone is resolved (and rejected with `Invalid configured local time zone '<id>': …`, exit `1`) before the output database is opened: no database file exists afterwards.
- [x] Failures from configuration, logging or opening/creating the database keep `Failed to initialize configuration, logging, or the output database: …`.
- [x] Failures after that point (discover, parse, aggregate, replace, summaries) report `Failed while harvesting the log files into the output database: …`. Proven with a read-only database file, which fails inside the replace.
- [x] All other messages and exit codes are unchanged; regression suite passes unmodified.

## Out of scope

- Discovering log files before opening the database (so "no log files found" would not leave an empty database). Deliberately **not** done: `Invoke-IisLogBackfill.ps1` calls `regenerate` unconditionally after the loop, including when every date failed, and `regenerate` refuses a database that does not exist. Creating the database before discovery is what lets that final `regenerate` still produce a (empty) Dashboard after an all-"no logs" backfill. Revisit together with the script if that behavior is no longer wanted.

## Comments

Follow-up to ticket 22. `HarvestRunner.Execute` keeps a `failureMessagePrefix` that starts as the initialization prefix and switches to the harvest prefix once the database is open and its schema exists. Tests first (both red before the change; the read-only-database case reproduced the old misleading "initialize" message): the existing invalid-time-zone test now also asserts the database file is not created, plus one new test for the post-initialization message (it clears the setup connection's SQLite pool first, otherwise the pooled read-write connection is reused and nothing fails). One consequence worth knowing: an *all-dates-failed-on-a-bad-time-zone* backfill no longer leaves an empty database behind, so its final `regenerate` now reports "does not exist" (exit 1) instead of building an empty Dashboard - arguably the more honest outcome for a configuration error. Smoke-tested the built exe with a bad time zone: exit 1, message as above, no `.sqlite` created.
