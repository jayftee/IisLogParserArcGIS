---
status: accepted
---

# Use SQLite as the aggregate store

The program is a console app invoked once per run (via Task Scheduler) to process one local day and write a replaceable daily batch of aggregate rows — a single-machine batch job, not a shared multi-writer service. The output database path is passed as a CLI argument.

We chose SQLite, a single portable file, over a server database (SQL Server/PostgreSQL). A server RDBMS was considered and rejected: it adds infrastructure and connection/deployment overhead that a once-a-day, single-writer batch job invoked from a console app doesn't need — the file itself can be copied, backed up, or handed off without a running server.
