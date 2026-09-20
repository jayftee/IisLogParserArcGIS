using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByRefererAndUriAggregatorTests
{
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithMultipleLinesForSamePair_SumsHitsAndTimeTakenSeconds()
    {
        var requests = new[]
        {
            Request(("https://example.com/", "/a"), timeTakenMilliseconds: 1000),
            Request(("https://example.com/", "/a"), timeTakenMilliseconds: 2000),
            Request(("https://example.com/", "/b"), timeTakenMilliseconds: 500),
        };

        var rows = ByRefererAndUriAggregator.Aggregate(requests, _targetDate);

        var rowForA = Assert.Single(rows, row => row.Referer == "https://example.com/" && row.UriStem == "/a");
        Assert.Equal(2, rowForA.Hits);
        Assert.Equal(3.0, rowForA.TimeTakenSecond);
        var rowForB = Assert.Single(rows, row => row.Referer == "https://example.com/" && row.UriStem == "/b");
        Assert.Equal(1, rowForB.Hits);
        Assert.Equal(0.5, rowForB.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_WithSameUriUnderTwoDifferentReferers_KeepsThemAsSeparateRows()
    {
        var requests = new[]
        {
            Request(("https://one.example/", "/shared")),
            Request(("https://two.example/", "/shared")),
        };

        var rows = ByRefererAndUriAggregator.Aggregate(requests, _targetDate);

        Assert.Equal(2, rows.Count);
        var rowForOne = Assert.Single(rows, row => row.Referer == "https://one.example/");
        Assert.Equal("/shared", rowForOne.UriStem);
        Assert.Equal(1, rowForOne.Hits);
        var rowForTwo = Assert.Single(rows, row => row.Referer == "https://two.example/");
        Assert.Equal("/shared", rowForTwo.UriStem);
        Assert.Equal(1, rowForTwo.Hits);
    }

    [Fact]
    public void Aggregate_WithSameRefererUnderTwoDifferentUris_KeepsThemAsSeparateRows()
    {
        var requests = new[]
        {
            Request(("https://shared.example/", "/one")),
            Request(("https://shared.example/", "/two")),
        };

        var rows = ByRefererAndUriAggregator.Aggregate(requests, _targetDate);

        Assert.Equal(2, rows.Count);
        var rowForOne = Assert.Single(rows, row => row.UriStem == "/one");
        Assert.Equal("https://shared.example/", rowForOne.Referer);
        Assert.Equal(1, rowForOne.Hits);
        var rowForTwo = Assert.Single(rows, row => row.UriStem == "/two");
        Assert.Equal("https://shared.example/", rowForTwo.Referer);
        Assert.Equal(1, rowForTwo.Hits);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates()
    {
        var requests = new[]
        {
            Request(("https://example.com/", "/a"), localDate: _targetDate),
            Request(("https://example.com/", "/a"), localDate: _targetDate.AddDays(1)),
        };

        var rows = ByRefererAndUriAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void Aggregate_ComposesRefererNormalization()
    {
        var requests = new[] { Request(("https://example.com/some+page", "/a")) };

        var rows = ByRefererAndUriAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("https://example.com/some page", row.Referer);
    }

    [Fact]
    public void Aggregate_ComposesUriStemTruncation()
    {
        var uriStem = "/" + new string('a', 1024);
        var requests = new[] { Request(("-", uriStem)) };

        var rows = ByRefererAndUriAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1024, row.UriStem.Length);
        Assert.Equal(uriStem[..1024], row.UriStem);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByRefererAndUriAggregator.Aggregate(null!, _targetDate));
    }

    private static NormalizedLogRequest Request((string Referer, string UriStem) key, int timeTakenMilliseconds = 0, DateOnly? localDate = null) =>
        new()
        {
            UtcDateTime = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            LocalDateTime = new DateTime(2026, 5, 1, 12, 0, 0),
            LocalDate = localDate ?? _targetDate,
            UriStem = key.UriStem,
            UserAgent = "-",
            Referer = key.Referer,
            Status = 200,
            TimeTakenMilliseconds = timeTakenMilliseconds,
            ForwardedFor = "-",
        };
}
