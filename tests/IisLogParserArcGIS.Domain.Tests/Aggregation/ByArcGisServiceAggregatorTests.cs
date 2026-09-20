using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByArcGisServiceAggregatorTests
{
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithMultipleHitsForSameService_SumsHitsAndTimeTakenSeconds()
    {
        var requests = new[]
        {
            Request("/mimas/rest/services/wildfire/firemap/MapServer/export", timeTakenMilliseconds: 1000),
            Request("/mimas/rest/services/wildfire/firemap/MapServer/query", timeTakenMilliseconds: 2000),
        };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("mimas", row.Site);
        Assert.Equal("wildfire", row.Folder);
        Assert.Equal("firemap", row.ServiceName);
        Assert.Equal("mapserver", row.ServiceType);
        Assert.Equal(2, row.Hits);
        Assert.Equal(3.0, row.SuccessfulTimeTakenSecond);
        Assert.Equal(0.0, row.FailedTimeTakenSecond);
    }

    [Fact]
    public void Aggregate_KeepsFolderlessServiceAsASeparateRowFromAFolderedServiceWithTheSameName()
    {
        var requests = new[]
        {
            Request("/mimas/rest/services/watermap/FeatureServer"),
            Request("/mimas/rest/services/east/watermap/FeatureServer"),
        };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        Assert.Equal(2, rows.Count);
        var folderless = Assert.Single(rows, row => row.Folder == null);
        Assert.Equal(1, folderless.Hits);
        var foldered = Assert.Single(rows, row => row.Folder == "east");
        Assert.Equal(1, foldered.Hits);
    }

    [Fact]
    public void Aggregate_GroupsRequestsForTheSameServiceRegardlessOfPathCasing()
    {
        var requests = new[]
        {
            Request("/titan/rest/services/Environment/Alberta_Watersheds/MapServer"),
            Request("/titan/rest/services/environment/alberta_watersheds/mapserver"),
        };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(2, row.Hits);
    }

    [Fact]
    public void Aggregate_ExcludesAdminTraffic()
    {
        var requests = new[] { Request("/mimas/admin/machines") };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        Assert.Empty(rows);
    }

    [Fact]
    public void Aggregate_ExcludesPortalTraffic()
    {
        var requests = new[] { Request("/portal/sharing/rest/portals/self") };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        Assert.Empty(rows);
    }

    [Fact]
    public void Aggregate_ExcludesPathsThatMerelyContainRestWithoutTheServiceShape()
    {
        var requests = new[] { Request("/test/service"), Request("/portal/sharing/rest/generateToken") };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        Assert.Empty(rows);
    }

    [Fact]
    public void Aggregate_SplitsHitsIntoSuccessfulAndFailedByStatusCode()
    {
        var requests = new[]
        {
            Request("/mimas/rest/services/wildfire/firemap/MapServer") with { Status = 200 },
            Request("/mimas/rest/services/wildfire/firemap/MapServer") with { Status = 304 },
            Request("/mimas/rest/services/wildfire/firemap/MapServer") with { Status = 404 },
            Request("/mimas/rest/services/wildfire/firemap/MapServer") with { Status = 500 },
        };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(4, row.Hits);
        Assert.Equal(2, row.SuccessfulHits);
        Assert.Equal(2, row.FailedHits);
        Assert.Equal(row.Hits, row.SuccessfulHits + row.FailedHits);
    }

    [Fact]
    public void Aggregate_SplitsTimeTakenBySuccessfulAndFailedIndependentlyOfHitCounts()
    {
        var requests = new[]
        {
            Request("/mimas/rest/services/wildfire/firemap/MapServer", timeTakenMilliseconds: 1000) with { Status = 200 },
            Request("/mimas/rest/services/wildfire/firemap/MapServer", timeTakenMilliseconds: 3000) with { Status = 200 },
            Request("/mimas/rest/services/wildfire/firemap/MapServer", timeTakenMilliseconds: 2000) with { Status = 500 },
        };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(4.0, row.SuccessfulTimeTakenSecond);
        Assert.Equal(2.0, row.FailedTimeTakenSecond);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates()
    {
        var requests = new[]
        {
            Request("/mimas/rest/services/wildfire/firemap/MapServer", localDate: _targetDate),
            Request("/mimas/rest/services/wildfire/firemap/MapServer", localDate: _targetDate.AddDays(1)),
        };

        var rows = ByArcGisServiceAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByArcGisServiceAggregator.Aggregate(null!, _targetDate));
    }

    private static NormalizedLogRequest Request(string uriStem, int timeTakenMilliseconds = 0, DateOnly? localDate = null) =>
        new()
        {
            UtcDateTime = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            LocalDateTime = new DateTime(2026, 5, 1, 12, 0, 0),
            LocalDate = localDate ?? _targetDate,
            UriStem = uriStem,
            UserAgent = "Mozilla/5.0",
            Referer = "-",
            Status = 200,
            TimeTakenMilliseconds = timeTakenMilliseconds,
            ForwardedFor = "-",
        };
}
