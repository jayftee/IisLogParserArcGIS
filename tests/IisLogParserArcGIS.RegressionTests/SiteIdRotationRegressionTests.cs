using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.RegressionTests.TestSupport;

namespace IisLogParserArcGIS.RegressionTests;

/// <summary>
/// Proves file discovery merges every rotating site-ID file active on the same UTC date, against a corpus date
/// confirmed to have three site-ID generations' files present simultaneously - not just the two files an
/// operator might assume are "the" pair for a day.
/// </summary>
public sealed class SiteIdRotationRegressionTests
{
    // 2026-05-11: six files exist for this UTC date, spanning three site-ID generations (10359/10360,
    // 11042/11043, 391/392) all active at once mid-rotation.
    private static readonly DateOnly _targetLocalDate = new(2026, 5, 11);

    [Fact]
    public async Task MergesEveryRotatingSiteIdFileActiveOnTheSameDate()
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
