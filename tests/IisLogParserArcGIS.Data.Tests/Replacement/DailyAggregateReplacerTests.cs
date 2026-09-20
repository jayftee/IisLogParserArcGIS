using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Replacement;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Data.Tests.Replacement;

public class DailyAggregateReplacerTests
{
    private static readonly DateOnly _priorDate = new(2026, 5, 1);
    private static readonly DateOnly _targetDate = new(2026, 5, 2);

    [Fact]
    public void Replace_InsertsFreshRowsAcrossAllTenTables()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);

        DailyAggregateReplacer.Replace(connection, BuildBatch(_targetDate, "first"));

        Assert.Single(new ByUriRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new ByRootRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new ByUserAgentRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new ByRefererRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new ByForwardedForIpRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new ByRefererAndUriRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new ByArcGisServiceRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new ByPortalItemRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _targetDate));
        Assert.Single(new BySurvey123DeviceRepository().GetByLocalDate(connection, _targetDate));
    }

    [Fact]
    public void Replace_CalledTwiceForTheSameDate_LeavesExactlyOneBatchPerTable()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);

        DailyAggregateReplacer.Replace(connection, BuildBatch(_targetDate, "first-run"));
        DailyAggregateReplacer.Replace(connection, BuildBatch(_targetDate, "second-run"));

        var byUriRows = new ByUriRepository().GetByLocalDate(connection, _targetDate).ToArray();
        Assert.Single(byUriRows);
        Assert.Equal("/second-run", byUriRows[0].UriStem);

        var byArcGisServiceRows = new ByArcGisServiceRepository().GetByLocalDate(connection, _targetDate).ToArray();
        Assert.Single(byArcGisServiceRows);
        Assert.Equal("second-run", byArcGisServiceRows[0].ServiceName);

        var byPortalItemRows = new ByPortalItemRepository().GetByLocalDate(connection, _targetDate).ToArray();
        Assert.Single(byPortalItemRows);
        Assert.Equal("second-run", byPortalItemRows[0].PortalItemId);

        var byFieldMapsDeviceRows = new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _targetDate).ToArray();
        Assert.Single(byFieldMapsDeviceRows);
        Assert.Equal("second-run", byFieldMapsDeviceRows[0].Username);

        var bySurvey123DeviceRows = new BySurvey123DeviceRepository().GetByLocalDate(connection, _targetDate).ToArray();
        Assert.Single(bySurvey123DeviceRows);
        Assert.Equal("second-run", bySurvey123DeviceRows[0].Username);
    }

    [Fact]
    public void Replace_BackfillsSurvey123RowsInBothHarvestOrders_WithoutTouchingTheFieldMapsTable()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var priorBatch = BuildBatch(_priorDate, "prior-day") with { BySurvey123Device = [Survey123Row(_priorDate, null)], ByFieldMapsDevice = [FieldMapsRow(_priorDate, null)] };
        DailyAggregateReplacer.Replace(connection, priorBatch);

        var targetBatch = BuildBatch(_targetDate, "target-day") with { BySurvey123Device = [Survey123Row(_targetDate, "user00305@somewhere")], ByFieldMapsDevice = [FieldMapsRow(_targetDate, null)] };
        DailyAggregateReplacer.Replace(connection, targetBatch);

        Assert.Equal("user00305@somewhere", Assert.Single(new BySurvey123DeviceRepository().GetByLocalDate(connection, _priorDate)).Username);
        Assert.Null(Assert.Single(new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _priorDate)).Username);
        Assert.Null(Assert.Single(new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _targetDate)).Username);
    }

    [Fact]
    public void Replace_BackfillsTheTargetDatesUnattributedRowsFromAnEarlierDatesLogin()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        DailyAggregateReplacer.Replace(connection, BuildBatch(_priorDate, "prior-day") with { ByFieldMapsDevice = [FieldMapsRow(_priorDate, "user00364@somewhere")] });

        DailyAggregateReplacer.Replace(connection, BuildBatch(_targetDate, "target-day") with { ByFieldMapsDevice = [FieldMapsRow(_targetDate, null)] });

        var targetRow = Assert.Single(new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _targetDate));
        Assert.Equal("user00364@somewhere", targetRow.Username);
    }

    [Fact]
    public void Replace_BackfillsAnEarlierDatesUnattributedRowsWhenALaterDateBringsTheLogin()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        DailyAggregateReplacer.Replace(connection, BuildBatch(_priorDate, "prior-day") with { ByFieldMapsDevice = [FieldMapsRow(_priorDate, null)] });
        Assert.Null(Assert.Single(new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _priorDate)).Username);

        DailyAggregateReplacer.Replace(connection, BuildBatch(_targetDate, "target-day") with { ByFieldMapsDevice = [FieldMapsRow(_targetDate, "user00364@somewhere")] });

        var priorRow = Assert.Single(new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _priorDate));
        Assert.Equal("user00364@somewhere", priorRow.Username);
    }

    [Fact]
    public void Replace_WhenAnInsertFailsPartway_RollsBackLeavingThePriorDayUntouchedInEveryTable()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);

        DailyAggregateReplacer.Replace(connection, BuildBatch(_priorDate, "prior-day"));

        var failingBatch = BuildBatch(_targetDate, "target-day") with
        {
            ByRefererAndUri =
            [
                new ByRefererAndUriAggregateRow
                {
                    LocalDate = _targetDate,
                    Referer = null!,
                    UriStem = "/target-day",
                    TimeTakenSecond = 1.0,
                    Hits = 1,
                },
            ],
        };

        Assert.Throws<SqliteException>(() => DailyAggregateReplacer.Replace(connection, failingBatch));

        AssertOnlyPriorDayRowsRemain(connection);
    }

    [Fact]
    public void Replace_BeforeCommitting_LeavesChangesInvisibleToAnotherConnection()
    {
        using var database = new TempSqliteDatabase();
        using (var setupConnection = SqliteConnectionFactory.Open(database.Path))
        {
            AggregateDatabaseSchema.EnsureCreated(setupConnection);
            DailyAggregateReplacer.Replace(setupConnection, BuildBatch(_priorDate, "prior-day"));
        }

        using var connection = SqliteConnectionFactory.Open(database.Path);
        var observedTargetDayRowCountBeforeCommit = -1;

        DailyAggregateReplacer.Replace(
            connection,
            BuildBatch(_targetDate, "target-day"),
            beforeCommit: _ =>
            {
                using var otherConnection = SqliteConnectionFactory.Open(database.Path);
                observedTargetDayRowCountBeforeCommit = new ByUriRepository().GetByLocalDate(otherConnection, _targetDate).Count();
                Assert.Single(new ByUriRepository().GetByLocalDate(otherConnection, _priorDate));
            });

        Assert.Equal(0, observedTargetDayRowCountBeforeCommit);
        Assert.Single(new ByUriRepository().GetByLocalDate(connection, _targetDate));
    }

    [Fact]
    public void Replace_WithNullConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DailyAggregateReplacer.Replace(null!, BuildBatch(_targetDate, "x")));
    }

    [Fact]
    public void Replace_WithNullBatch_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);

        Assert.Throws<ArgumentNullException>(() => DailyAggregateReplacer.Replace(connection, null!));
    }

    private static void AssertOnlyPriorDayRowsRemain(SqliteConnection connection)
    {
        Assert.Single(new ByUriRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new ByRootRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new ByUserAgentRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new ByRefererRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new ByForwardedForIpRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new ByRefererAndUriRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new ByArcGisServiceRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new ByPortalItemRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _priorDate));
        Assert.Single(new BySurvey123DeviceRepository().GetByLocalDate(connection, _priorDate));

        Assert.Empty(new ByUriRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new ByRootRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new ByUserAgentRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new ByRefererRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new ByForwardedForIpRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new ByRefererAndUriRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new ByArcGisServiceRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new ByPortalItemRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new ByFieldMapsDeviceRepository().GetByLocalDate(connection, _targetDate));
        Assert.Empty(new BySurvey123DeviceRepository().GetByLocalDate(connection, _targetDate));
    }

    private static DailyAggregateBatch BuildBatch(DateOnly localDate, string label) => new()
    {
        LocalDate = localDate,
        ByUri =
        [
            new ByUriAggregateRow { LocalDate = localDate, UriStem = $"/{label}", TimeTakenSecond = 1.0, Hits = 1 },
        ],
        ByRoot =
        [
            new ByRootAggregateRow { LocalDate = localDate, Root = label, TimeTakenSecond = 1.0, Hits = 1 },
        ],
        ByUserAgent =
        [
            new ByUserAgentAggregateRow { LocalDate = localDate, UserAgent = label, TimeTakenSecond = 1.0, Hits = 1 },
        ],
        ByReferer =
        [
            new ByRefererAggregateRow { LocalDate = localDate, Referer = label, TimeTakenSecond = 1.0, Hits = 1 },
        ],
        ByForwardedForIp =
        [
            new ByForwardedForIpAggregateRow { LocalDate = localDate, ForwardedForIp = "127.0.0.1", TimeTakenSecond = 1.0, Hits = 1 },
        ],
        ByRefererAndUri =
        [
            new ByRefererAndUriAggregateRow { LocalDate = localDate, Referer = label, UriStem = $"/{label}", TimeTakenSecond = 1.0, Hits = 1 },
        ],
        ByArcGisService = [BuildArcGisServiceRow(localDate, label)],
        ByPortalItem = [BuildPortalItemRow(localDate, label)],
        ByFieldMapsDevice = [FieldMapsRow(localDate, label)],
        BySurvey123Device = [Survey123Row(localDate, label)],
    };

    private static BySurvey123DeviceAggregateRow Survey123Row(DateOnly localDate, string? username) => new()
    {
        LocalDate = localDate,
        DeviceId = "0123456789abcdef0123456789abcdef",
        Username = username,
        TimeTakenSecond = 1.0,
        Hits = 1,
    };

    private static ByFieldMapsDeviceAggregateRow FieldMapsRow(DateOnly localDate, string? username) => new()
    {
        LocalDate = localDate,
        DeviceId = "227c3d43-ea74-4d05-aaca-1e47ac4bcde9",
        Username = username,
        TimeTakenSecond = 1.0,
        Hits = 1,
    };

    private static ByArcGisServiceAggregateRow BuildArcGisServiceRow(DateOnly localDate, string label) => new()
    {
        LocalDate = localDate,
        Site = "site",
        Folder = null,
        ServiceName = label,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = 1.0,
        FailedTimeTakenSecond = 0.0,
        Hits = 1,
        SuccessfulHits = 1,
        FailedHits = 0,
    };

    private static ByPortalItemAggregateRow BuildPortalItemRow(DateOnly localDate, string label) => new()
    {
        LocalDate = localDate,
        PortalItemId = label,
        TimeTakenSecond = 1.0,
        Hits = 1,
        SuccessfulHits = 1,
        FailedHits = 0,
    };
}
