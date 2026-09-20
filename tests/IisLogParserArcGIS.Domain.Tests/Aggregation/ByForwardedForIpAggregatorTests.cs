using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByForwardedForIpAggregatorTests
{
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithMultipleLinesForSameIp_SumsHitsAndTimeTakenSeconds()
    {
        var requests = new[]
        {
            Request("203.0.113.1", timeTakenMilliseconds: 1000),
            Request("203.0.113.1", timeTakenMilliseconds: 2000),
            Request("70.41.3.18", timeTakenMilliseconds: 500),
        };

        var rows = ByForwardedForIpAggregator.Aggregate(requests, _targetDate);

        var rowForFirst = Assert.Single(rows, row => row.ForwardedForIp == "203.0.113.1");
        Assert.Equal(2, rowForFirst.Hits);
        Assert.Equal(3.0, rowForFirst.TimeTakenSecond);
        var rowForSecond = Assert.Single(rows, row => row.ForwardedForIp == "70.41.3.18");
        Assert.Equal(1, rowForSecond.Hits);
        Assert.Equal(0.5, rowForSecond.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates()
    {
        var requests = new[]
        {
            Request("203.0.113.1", localDate: _targetDate),
            Request("203.0.113.1", localDate: _targetDate.AddDays(1)),
        };

        var rows = ByForwardedForIpAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void Aggregate_WithMultiValueForwardedForList_GroupsUnderFirstIp()
    {
        var requests = new[]
        {
            Request("203.0.113.1, 70.41.3.18"),
            Request("203.0.113.1, 150.172.238.178"),
        };

        var rows = ByForwardedForIpAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("203.0.113.1", row.ForwardedForIp);
        Assert.Equal(2, row.Hits);
    }

    [Fact]
    public void Aggregate_WithPlusEncodedForwardedFor_GroupsUnderDecodedFirstIp()
    {
        var requests = new[] { Request("203.0.113.1,+70.41.3.18") };

        var rows = ByForwardedForIpAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("203.0.113.1", row.ForwardedForIp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    public void Aggregate_WithAbsentForwardedFor_GroupsUnderLoopbackPlaceholder(string forwardedFor)
    {
        var requests = new[] { Request(forwardedFor) };

        var rows = ByForwardedForIpAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("127.0.0.1", row.ForwardedForIp);
    }

    [Fact]
    public void Aggregate_WithInvalidIp_GroupsUnderDash()
    {
        var requests = new[] { Request("not-an-ip") };

        var rows = ByForwardedForIpAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("-", row.ForwardedForIp);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByForwardedForIpAggregator.Aggregate(null!, _targetDate));
    }

    [Fact]
    public void Aggregate_WithRequestWhoseGoverningHeaderDidNotDeclareForwardedFor_SilentlyExcludesIt()
    {
        var requests = new[]
        {
            Request("203.0.113.1"),
            Request(null),
        };

        var rows = ByForwardedForIpAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("203.0.113.1", row.ForwardedForIp);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void Aggregate_WithOnlyUndeclaredForwardedFor_ReturnsNoRows()
    {
        var requests = new[] { Request(null) };

        var rows = ByForwardedForIpAggregator.Aggregate(requests, _targetDate);

        Assert.Empty(rows);
    }

    private static NormalizedLogRequest Request(string? forwardedFor, int timeTakenMilliseconds = 0, DateOnly? localDate = null) =>
        new()
        {
            UtcDateTime = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            LocalDateTime = new DateTime(2026, 5, 1, 12, 0, 0),
            LocalDate = localDate ?? _targetDate,
            UriStem = "/a",
            UserAgent = "-",
            Referer = "-",
            Status = 200,
            TimeTakenMilliseconds = timeTakenMilliseconds,
            ForwardedFor = forwardedFor,
        };
}
