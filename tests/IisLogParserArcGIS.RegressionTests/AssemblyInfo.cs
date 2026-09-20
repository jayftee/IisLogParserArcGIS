using System.Runtime.CompilerServices;

// Each test spawns the real compiled executable against the same real corpus directory on disk and shares a
// process-wide SQLite connection pool (see TempOutputDatabase.Dispose); running scenarios serially keeps
// timing predictable and avoids one test's pool teardown racing another's in-flight connection.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// The Reports.Tests project's harvested-aggregate-database fixture (ticket 09) replays this project's own
// CompiledProgram/ProcessRunner tooling against the same real 2026/ corpus and shipped executable, rather than
// duplicating it - see IisLogParserArcGIS.Reports.Tests.TestSupport.HarvestedAggregateDatabaseFixture.
[assembly: InternalsVisibleTo("IisLogParserArcGIS.Reports.Tests")]
