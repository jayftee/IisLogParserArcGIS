---
status: accepted
---

# Root allow-listing lives in the Reports project, not the parser

IIS logs inevitably include requests from scanners, pen testers, and other hosts nobody wants reported on. We considered filtering these out at ingest time, in the existing parser CLI, so that traffic never reaches the aggregate tables. Instead, the list of Included Roots lives in the new Reports project's own configuration and is applied at Regeneration Run time; the parser keeps aggregating every Root it sees, with no allow-list of its own.

This keeps the aggregate database a complete record of everything IIS actually received — useful if the set of Roots worth reporting on ever changes, or for later security review — and keeps "who currently cares about this host" a reporting-layer concern, separate from the always-running ingest pipeline described in [ADR-0001](0001-sqlite-for-aggregate-store.md).
