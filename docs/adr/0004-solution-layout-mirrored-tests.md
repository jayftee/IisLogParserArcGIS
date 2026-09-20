---
status: accepted
---

# Mirrored src/tests project layout, with a separate non-mirrored regression project

The solution has one `src/` project per layer (Domain — pure parsing/aggregation logic; Data — SQLite schema, Repositories, and Query classes; the Console host — composition root, CLI, config, logging) plus the full-year `2026/` corpus as an end-to-end regression fixture set.

We mirror every `src/` project with a matching xUnit project under `tests/`, including 1:1 class-to-test-class naming, so coverage gaps are visually obvious by comparing the two trees. The full-corpus regression run against real data doesn't correspond to any single class — it exercises all three layers together — so it lives in its own separate project instead of being force-fit into one of the mirrored per-layer test projects. This keeps the fast, mirrored unit tests uncluttered and keeps the 1:1 convention exception-free.
