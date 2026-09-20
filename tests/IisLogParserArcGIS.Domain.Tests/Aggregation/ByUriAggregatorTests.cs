using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByUriAggregatorTests
{
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithMultipleLinesForSameUri_SumsHitsAndTimeTakenSeconds()
    {
        var requests = new[]
        {
            Request("/a", timeTakenMilliseconds: 1000),
            Request("/a", timeTakenMilliseconds: 2000),
            Request("/b", timeTakenMilliseconds: 500),
        };

        var rows = ByUriAggregator.Aggregate(requests, _targetDate);

        var rowForA = Assert.Single(rows, row => row.UriStem == "/a");
        Assert.Equal(2, rowForA.Hits);
        Assert.Equal(3.0, rowForA.TimeTakenSecond);
        var rowForB = Assert.Single(rows, row => row.UriStem == "/b");
        Assert.Equal(1, rowForB.Hits);
        Assert.Equal(0.5, rowForB.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates()
    {
        var requests = new[]
        {
            Request("/a", localDate: _targetDate),
            Request("/a", localDate: _targetDate.AddDays(1)),
        };

        var rows = ByUriAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void Aggregate_WithUriStemAtThe1024CharacterBoundary_IsNotTruncated()
    {
        var uriStem = "/" + new string('a', 1023);
        var requests = new[] { Request(uriStem) };

        var rows = ByUriAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1024, row.UriStem.Length);
        Assert.Equal(uriStem, row.UriStem);
    }

    [Fact]
    public void Aggregate_WithUriStemOverThe1024CharacterBoundary_TruncatesTo1024Characters()
    {
        var uriStem = "/" + new string('a', 1024);
        var requests = new[] { Request(uriStem) };

        var rows = ByUriAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1024, row.UriStem.Length);
        Assert.Equal(uriStem[..1024], row.UriStem);
    }

    [Fact]
    public void Aggregate_WithTwoUrisTruncatingToTheSameStem_MergesThemIntoOneRow()
    {
        var commonPrefix = new string('a', 1024);
        var requests = new[]
        {
            Request(commonPrefix + "1"),
            Request(commonPrefix + "2"),
        };

        var rows = ByUriAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(2, row.Hits);
        Assert.Equal(commonPrefix, row.UriStem);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByUriAggregator.Aggregate(null!, _targetDate));
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
