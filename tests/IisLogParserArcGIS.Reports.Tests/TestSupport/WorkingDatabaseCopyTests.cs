using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Reports.Tests.TestSupport;

/// <summary>
/// Proves <see cref="WorkingDatabaseCopy"/> lets a test insert edge-case rows the real corpus doesn't naturally
/// contain - an exact ranking tie, an entity present for only part of the date range - without mutating the
/// harvested database every other test in the collection reads from.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class WorkingDatabaseCopyTests
{
    private static readonly DateOnly _edgeCaseDate = new(2026, 1, 1);

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public WorkingDatabaseCopyTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InsertArcGisServiceRows_CreatesAnExactRankingTie_VisibleOnlyOnTheCopy()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);

        workingCopy.InsertArcGisServiceRows(
            CreateServiceRow("TiedServiceA", hits: 50),
            CreateServiceRow("TiedServiceB", hits: 50));

        using var copyConnection = workingCopy.OpenConnection();
        var tiedHits = new ByArcGisServiceRepository().GetByLocalDate(copyConnection, _edgeCaseDate)
            .Where(row => row.Site == "edgecase")
            .Select(row => row.Hits)
            .ToArray();

        Assert.Equal([50, 50], tiedHits);

        using var harvestedConnection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var harvestedHasEdgeCase = new ByArcGisServiceRepository().GetByLocalDate(harvestedConnection, _edgeCaseDate)
            .Any(row => row.Site == "edgecase");

        Assert.False(harvestedHasEdgeCase);
    }

    [Fact]
    public void InsertPortalItemRows_AddsAnItemPresentForOnlyPartOfTheDateRange()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        const string EdgeCaseItemId = "edgecase-partial-range-item";

        workingCopy.InsertPortalItemRows(new ByPortalItemAggregateRow
        {
            LocalDate = _edgeCaseDate,
            PortalItemId = EdgeCaseItemId,
            TimeTakenSecond = 2.5,
            Hits = 10,
            SuccessfulHits = 10,
            FailedHits = 0,
        });

        using var copyConnection = workingCopy.OpenConnection();
        var insertedRow = new ByPortalItemRepository().GetByLocalDate(copyConnection, _edgeCaseDate)
            .Single(row => row.PortalItemId == EdgeCaseItemId);

        Assert.Equal(10, insertedRow.Hits);

        var laterDate = _edgeCaseDate.AddDays(1);
        using var harvestedConnection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var laterDateHasEdgeCaseItem = new ByPortalItemRepository().GetByLocalDate(harvestedConnection, laterDate)
            .Any(row => row.PortalItemId == EdgeCaseItemId);

        Assert.False(laterDateHasEdgeCaseItem);
    }

    private static ByArcGisServiceAggregateRow CreateServiceRow(string serviceName, int hits)
    {
        return new ByArcGisServiceAggregateRow
        {
            LocalDate = _edgeCaseDate,
            Site = "edgecase",
            Folder = null,
            ServiceName = serviceName,
            ServiceType = "MapServer",
            SuccessfulTimeTakenSecond = 1.0,
            FailedTimeTakenSecond = 0.0,
            Hits = hits,
            SuccessfulHits = hits,
            FailedHits = 0,
        };
    }
}
