using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByPortalItemAggregatorTests
{
    private const string WebAdaptorName = "portal";
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithMultipleHitsForSameItem_SumsHitsAndTimeTakenSeconds()
    {
        var requests = new[]
        {
            Request("/portal/sharing/rest/content/items/abc123/data", timeTakenMilliseconds: 1000),
            Request("/portal/sharing/rest/content/items/abc123/info/thumbnail/thumbnail.png", timeTakenMilliseconds: 2000),
        };

        var rows = ByPortalItemAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal("abc123", row.PortalItemId);
        Assert.Equal(2, row.Hits);
        Assert.Equal(3.0, row.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_GroupsUserScopedAndDirectAccessOfTheSameItemIntoOneRow()
    {
        var requests = new[]
        {
            Request("/portal/sharing/rest/content/items/abc123"),
            Request("/portal/sharing/rest/content/users/jsmith/items/abc123"),
            Request("/portal/sharing/rest/content/users/jsmith/f00lder/items/abc123"),
        };

        var rows = ByPortalItemAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(3, row.Hits);
    }

    [Fact]
    public void Aggregate_ExcludesUserScopedBulkOperationsWithNoItemId()
    {
        var requests = new[]
        {
            Request("/portal/sharing/rest/content/users/jsmith/deleteItems"),
            Request("/portal/sharing/rest/content/users/jsmith/shareItems"),
        };

        var rows = ByPortalItemAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Empty(rows);
    }

    [Fact]
    public void Aggregate_SplitsHitsIntoSuccessfulAndFailedByStatusCode()
    {
        var requests = new[]
        {
            Request("/portal/sharing/rest/content/items/abc123") with { Status = 200 },
            Request("/portal/sharing/rest/content/items/abc123") with { Status = 304 },
            Request("/portal/sharing/rest/content/items/abc123") with { Status = 404 },
            Request("/portal/sharing/rest/content/items/abc123") with { Status = 500 },
        };

        var rows = ByPortalItemAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(4, row.Hits);
        Assert.Equal(2, row.SuccessfulHits);
        Assert.Equal(2, row.FailedHits);
        Assert.Equal(row.Hits, row.SuccessfulHits + row.FailedHits);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates()
    {
        var requests = new[]
        {
            Request("/portal/sharing/rest/content/items/abc123", localDate: _targetDate),
            Request("/portal/sharing/rest/content/items/abc123", localDate: _targetDate.AddDays(1)),
        };

        var rows = ByPortalItemAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void Aggregate_WithNonDefaultConfiguredWebAdaptorName_HonorsTheConfiguredName()
    {
        var requests = new[] { Request("/gis/sharing/rest/content/items/abc123") };

        var rows = ByPortalItemAggregator.Aggregate(requests, _targetDate, "gis");

        var row = Assert.Single(rows);
        Assert.Equal("abc123", row.PortalItemId);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByPortalItemAggregator.Aggregate(null!, _targetDate, WebAdaptorName));
    }

    [Fact]
    public void Aggregate_WithNullPortalWebAdaptorName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByPortalItemAggregator.Aggregate([], _targetDate, null!));
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
