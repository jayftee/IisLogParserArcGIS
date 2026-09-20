using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByArcGisServiceRangeTotalsQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Fact]
    public void GetRangeTotals_SumsHitsAndTimeAcrossTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(new DateOnly(2026, 5, 1), "mimas", "firemap", successfulHits: 2, failedHits: 1, successfulTime: 2.0, failedTime: 1.0),
                ServiceRow(new DateOnly(2026, 5, 2), "mimas", "firemap", successfulHits: 3, failedHits: 0, successfulTime: 3.0, failedTime: 0.0),
            ]);
        var query = new ByArcGisServiceRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2), ["mimas"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("mimas", row.Site);
        Assert.Equal("firemap", row.ServiceName);
        Assert.Equal(6, row.Hits);
        Assert.Equal(5, row.SuccessfulHits);
        Assert.Equal(1, row.FailedHits);
        Assert.Equal(6.0, row.TotalTimeTakenSecond);
    }

    [Fact]
    public void GetRangeTotals_ReturnsOneRowPerFolderServiceTypeCombinationAcrossEveryIncludedSite()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(_localDate, "mimas", "firemap"),
                ServiceRow(_localDate, "mimas", "watermap"),
                ServiceRow(_localDate, "titan", "landmap"),
            ]);
        var query = new ByArcGisServiceRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(_localDate, _localDate, ["mimas", "titan"]).ToArray();

        Assert.Equal(3, rows.Length);
        Assert.Contains(rows, row => row.Site == "mimas" && row.ServiceName == "firemap");
        Assert.Contains(rows, row => row.Site == "mimas" && row.ServiceName == "watermap");
        Assert.Contains(rows, row => row.Site == "titan" && row.ServiceName == "landmap");
    }

    [Fact]
    public void GetRangeTotals_ReturnsOnlyIncludedRoots()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(_localDate, "mimas", "firemap"),
                ServiceRow(_localDate, "proxy", "firemap"),
            ]);
        var query = new ByArcGisServiceRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(_localDate, _localDate, ["mimas"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("mimas", row.Site);
    }

    [Fact]
    public void GetRangeTotals_ExcludesRowsOutsideTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(new DateOnly(2025, 12, 31), "mimas", "firemap"),
                ServiceRow(new DateOnly(2026, 1, 1), "mimas", "firemap"),
            ]);
        var query = new ByArcGisServiceRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), ["mimas"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void GetRangeTotals_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(_localDate, _localDate, ["mimas"]);

        Assert.Empty(rows);
    }

    [Fact]
    public void GetRangeTotals_WithNullIncludedRoots_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceRangeTotalsQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetRangeTotals(_localDate, _localDate, null!));
    }

#pragma warning disable CC0042 // Six independent test-fixture inputs; a parameter object would just repackage them without benefit.
    private static ByArcGisServiceAggregateRow ServiceRow(
        DateOnly localDate, string site, string serviceName, int successfulHits = 1, int failedHits = 0, double successfulTime = 1.0, double failedTime = 0.0) => new()
#pragma warning restore CC0042
    {
        LocalDate = localDate,
        Site = site,
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = successfulTime,
        FailedTimeTakenSecond = failedTime,
        Hits = successfulHits + failedHits,
        SuccessfulHits = successfulHits,
        FailedHits = failedHits,
    };
}
