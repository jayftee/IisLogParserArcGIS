using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class ByArcGisServiceRepositoryTests
{
    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsRowsIncludingNullFolder()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByArcGisServiceRepository();
        var localDate = new DateOnly(2026, 5, 1);

        repository.Insert(
            connection,
            [
                new ByArcGisServiceAggregateRow
                {
                    LocalDate = localDate,
                    Site = "mimas",
                    Folder = "wildfire",
                    ServiceName = "firemap",
                    ServiceType = "MapServer",
                    SuccessfulTimeTakenSecond = 1.0,
                    FailedTimeTakenSecond = 0.5,
                    Hits = 3,
                    SuccessfulHits = 2,
                    FailedHits = 1,
                },
                new ByArcGisServiceAggregateRow
                {
                    LocalDate = localDate,
                    Site = "mimas",
                    Folder = null,
                    ServiceName = "watermap",
                    ServiceType = "FeatureServer",
                    SuccessfulTimeTakenSecond = 2.0,
                    FailedTimeTakenSecond = 0.0,
                    Hits = 4,
                    SuccessfulHits = 4,
                    FailedHits = 0,
                },
            ]);

        var rows = repository.GetByLocalDate(connection, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        var foldered = Assert.Single(rows, row => row.ServiceName == "firemap");
        Assert.Equal("wildfire", foldered.Folder);
        Assert.Equal(1.0, foldered.SuccessfulTimeTakenSecond);
        Assert.Equal(0.5, foldered.FailedTimeTakenSecond);
        Assert.Equal(3, foldered.Hits);
        Assert.Equal(2, foldered.SuccessfulHits);
        Assert.Equal(1, foldered.FailedHits);
        var folderless = Assert.Single(rows, row => row.ServiceName == "watermap");
        Assert.Null(folderless.Folder);
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByArcGisServiceRepository();
        repository.Insert(
            connection,
            [
                new ByArcGisServiceAggregateRow
                {
                    LocalDate = new DateOnly(2026, 5, 1),
                    Site = "mimas",
                    Folder = null,
                    ServiceName = "watermap",
                    ServiceType = "FeatureServer",
                    SuccessfulTimeTakenSecond = 1.0,
                    FailedTimeTakenSecond = 0.0,
                    Hits = 1,
                    SuccessfulHits = 1,
                    FailedHits = 0,
                },
                new ByArcGisServiceAggregateRow
                {
                    LocalDate = new DateOnly(2026, 5, 2),
                    Site = "mimas",
                    Folder = null,
                    ServiceName = "watermap",
                    ServiceType = "FeatureServer",
                    SuccessfulTimeTakenSecond = 1.0,
                    FailedTimeTakenSecond = 0.0,
                    Hits = 1,
                    SuccessfulHits = 1,
                    FailedHits = 0,
                },
            ]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }
}
