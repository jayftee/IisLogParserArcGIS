---
status: accepted
---

# Dapper with a Repository/Query split for data access

There are ten aggregate tables, each with simple, well-defined CRUD (replace a local day's rows), plus room for non-CRUD or cross-aggregate reporting queries as needs grow.

We chose Dapper for all SQL execution, with CRUD for each aggregate table living behind a Repository dedicated to that entity, and non-CRUD/cross-cutting queries living in separate Query classes outside any repository rather than bolted onto repository interfaces. EF Core was considered and rejected: the schema is fixed and simple, writes are whole-day replaces rather than incremental changes, and change-tracking overhead buys nothing here — hand-written SQL keeps the replace semantics explicit. Keeping ad hoc queries out of the repositories keeps each Repository scoped to the entity it owns instead of accumulating unrelated read methods.
