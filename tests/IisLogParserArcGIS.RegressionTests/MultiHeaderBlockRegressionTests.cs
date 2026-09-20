using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.RegressionTests.TestSupport;

namespace IisLogParserArcGIS.RegressionTests;

/// <summary>
/// Proves the shipped executable keeps tracking the most-recently-seen <c>#Fields</c> header within a single
/// physical file, against a corpus file confirmed to restart its header block several times in one day (an IIS
/// logging restart mid-day).
/// </summary>
public sealed class MultiHeaderBlockRegressionTests
{
    // 2026-06-08: one of the day's site-ID files restarts its #Fields header block six times.
    private static readonly DateOnly _targetLocalDate = new(2026, 6, 8);

    [Fact]
    public async Task ParsesEveryHeaderBlockWithinASingleDaysFiles()
    {
        using var database = new TempOutputDatabase();

        var summary = await RegressionAssertions.RunAndAssertCoreInvariantsAsync(_targetLocalDate, database.Path, connection =>
        {
            var arcGisServiceRows = new ByArcGisServiceRepository().GetByLocalDate(connection, _targetLocalDate);
            Assert.NotEmpty(arcGisServiceRows);
        });

        Assert.Equal(0, summary.Invalid);
    }
}
