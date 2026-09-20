---
status: accepted
---

# Optional ILoggerFactory constructor parameter, falling back to NullLoggerFactory

Every class that logs takes an optional `ILoggerFactory` constructor parameter. When it's null, the class falls back to `Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance` and creates its logger from that, rather than requiring a real factory via DI or checking for null at every log call site.

We chose this over the more conventional "always inject `ILogger<T>` via the DI container" approach specifically for testability: every log call site is unconditionally safe (a `NullLogger` silently no-ops, so there's nothing to null-check), and unit tests can construct any class by passing `null` for the logger with zero logging-provider setup — minimizing test boilerplate project-wide.
