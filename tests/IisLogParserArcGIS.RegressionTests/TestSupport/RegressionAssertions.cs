using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// Runs the shipped executable for one target local date and asserts the invariants every regression scenario
/// shares - process exit code, the reported total matching an independently-computed oracle, and the by-URI
/// aggregate's hit count matching the oracle's per-date line count - so each scenario's own test only needs to
/// add what makes it distinct.
/// </summary>
internal static class RegressionAssertions
{
    /// <summary>
    /// Runs the shipped executable for <paramref name="targetLocalDate"/> and asserts the shared invariants.
    /// </summary>
    /// <param name="targetLocalDate">The target local date to process.</param>
    /// <param name="outputDatabasePath">The output SQLite database path.</param>
    /// <param name="assertAdditional">
    /// An optional callback given the open output connection, for a scenario's own additional assertions.
    /// </param>
    /// <returns>The parsed run summary, for a scenario's own additional assertions (e.g. on <c>Invalid</c>).</returns>
    public static async Task<RunSummary> RunAndAssertCoreInvariantsAsync(
        DateOnly targetLocalDate, string outputDatabasePath, Action<SqliteConnection>? assertAdditional = null)
    {
        var result = await CompiledProgram.RunAsync(targetLocalDate, outputDatabasePath).ConfigureAwait(false);

        Assert.Equal(0, result.ExitCode);

        var summary = RunSummaryParser.Parse(result.StandardOutput);
        var expectedCounts = RawCorpusLineCounter.Count(CompiledProgram.CorpusDirectory, targetLocalDate);

        Assert.Equal(expectedCounts.Total, summary.Total);

        using var connection = SqliteConnectionFactory.Open(outputDatabasePath);
        var byUriHits = new ByUriRepository().GetByLocalDate(connection, targetLocalDate).Sum(row => row.Hits);

        Assert.Equal(expectedCounts.MatchingDate, byUriHits);

        assertAdditional?.Invoke(connection);

        return summary;
    }
}
