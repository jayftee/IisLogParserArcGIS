using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByUserAgentAggregatorTests
{
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithMultipleLinesForSameUserAgent_SumsHitsAndTimeTakenSeconds()
    {
        var requests = new[]
        {
            Request("Mozilla/5.0", timeTakenMilliseconds: 1000),
            Request("Mozilla/5.0", timeTakenMilliseconds: 2000),
            Request("curl/8.0", timeTakenMilliseconds: 500),
        };

        var rows = ByUserAgentAggregator.Aggregate(requests, _targetDate);

        var rowForMozilla = Assert.Single(rows, row => row.UserAgent == "mozilla/5.0");
        Assert.Equal(2, rowForMozilla.Hits);
        Assert.Equal(3.0, rowForMozilla.TimeTakenSecond);
        var rowForCurl = Assert.Single(rows, row => row.UserAgent == "curl/8.0");
        Assert.Equal(1, rowForCurl.Hits);
        Assert.Equal(0.5, rowForCurl.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates()
    {
        var requests = new[]
        {
            Request("Mozilla/5.0", localDate: _targetDate),
            Request("Mozilla/5.0", localDate: _targetDate.AddDays(1)),
        };

        var rows = ByUserAgentAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void Aggregate_WithPlusEncodedUserAgent_DecodesPlusToSpace()
    {
        var requests = new[] { Request("Mozilla/5.0+(Windows+NT+10.0)") };

        var rows = ByUserAgentAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("mozilla/5.0 (windows nt 10.0)", row.UserAgent);
    }

    [Fact]
    public void Aggregate_WithMixedCaseUserAgent_Lowercases()
    {
        var requests = new[] { Request("Mozilla/5.0 (Windows NT 10.0)") };

        var rows = ByUserAgentAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("mozilla/5.0 (windows nt 10.0)", row.UserAgent);
    }

    [Fact]
    public void Aggregate_WithUserAgentAtThe1024CharacterBoundary_IsNotTruncated()
    {
        var userAgent = new string('a', 1024);
        var requests = new[] { Request(userAgent) };

        var rows = ByUserAgentAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1024, row.UserAgent.Length);
        Assert.Equal(userAgent, row.UserAgent);
    }

    [Fact]
    public void Aggregate_WithUserAgentOverThe1024CharacterBoundary_TruncatesTo1024Characters()
    {
        var userAgent = new string('a', 1025);
        var requests = new[] { Request(userAgent) };

        var rows = ByUserAgentAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1024, row.UserAgent.Length);
        Assert.Equal(userAgent[..1024], row.UserAgent);
    }

    [Fact]
    public void Aggregate_WithTwoUserAgentsTruncatingToTheSameValue_MergesThemIntoOneRow()
    {
        var commonPrefix = new string('a', 1024);
        var requests = new[]
        {
            Request(commonPrefix + "1"),
            Request(commonPrefix + "2"),
        };

        var rows = ByUserAgentAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(2, row.Hits);
        Assert.Equal(commonPrefix, row.UserAgent);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByUserAgentAggregator.Aggregate(null!, _targetDate));
    }

    private static NormalizedLogRequest Request(string userAgent, int timeTakenMilliseconds = 0, DateOnly? localDate = null) =>
        new()
        {
            UtcDateTime = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            LocalDateTime = new DateTime(2026, 5, 1, 12, 0, 0),
            LocalDate = localDate ?? _targetDate,
            UriStem = "/a",
            UserAgent = userAgent,
            Referer = "-",
            Status = 200,
            TimeTakenMilliseconds = timeTakenMilliseconds,
            ForwardedFor = "-",
        };
}
