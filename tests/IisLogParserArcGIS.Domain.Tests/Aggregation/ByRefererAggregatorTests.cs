using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByRefererAggregatorTests
{
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithMultipleLinesForSameReferer_SumsHitsAndTimeTakenSeconds()
    {
        var requests = new[]
        {
            Request("https://example.com/", timeTakenMilliseconds: 1000),
            Request("https://example.com/", timeTakenMilliseconds: 2000),
            Request("https://other.example/", timeTakenMilliseconds: 500),
        };

        var rows = ByRefererAggregator.Aggregate(requests, _targetDate);

        var rowForExample = Assert.Single(rows, row => row.Referer == "https://example.com/");
        Assert.Equal(2, rowForExample.Hits);
        Assert.Equal(3.0, rowForExample.TimeTakenSecond);
        var rowForOther = Assert.Single(rows, row => row.Referer == "https://other.example/");
        Assert.Equal(1, rowForOther.Hits);
        Assert.Equal(0.5, rowForOther.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates()
    {
        var requests = new[]
        {
            Request("https://example.com/", localDate: _targetDate),
            Request("https://example.com/", localDate: _targetDate.AddDays(1)),
        };

        var rows = ByRefererAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    public void Aggregate_WithEmptyOrDashReferer_GroupsUnderDash(string referer)
    {
        var requests = new[] { Request(referer) };

        var rows = ByRefererAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("-", row.Referer);
    }

    [Fact]
    public void Aggregate_WithPlusEncodedReferer_DecodesPlusToSpace()
    {
        var requests = new[] { Request("https://example.com/some+page") };

        var rows = ByRefererAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("https://example.com/some page", row.Referer);
    }

    [Fact]
    public void Aggregate_WithOpaqueNonUrlReferer_AcceptsAsIs()
    {
        var requests = new[] { Request("com.esri.arcgis.rest.SomeInternalHandler") };

        var rows = ByRefererAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("com.esri.arcgis.rest.someinternalhandler", row.Referer);
    }

    [Fact]
    public void Aggregate_WithRefererAtThe4096CharacterBoundary_IsNotTruncated()
    {
        var referer = new string('a', 4096);
        var requests = new[] { Request(referer) };

        var rows = ByRefererAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(4096, row.Referer.Length);
        Assert.Equal(referer, row.Referer);
    }

    [Fact]
    public void Aggregate_WithRefererOverThe4096CharacterBoundary_TruncatesTo4096Characters()
    {
        var referer = new string('a', 4097);
        var requests = new[] { Request(referer) };

        var rows = ByRefererAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(4096, row.Referer.Length);
        Assert.Equal(referer[..4096], row.Referer);
    }

    [Fact]
    public void Aggregate_WithTwoReferersTruncatingToTheSameValue_MergesThemIntoOneRow()
    {
        var commonPrefix = new string('a', 4096);
        var requests = new[]
        {
            Request(commonPrefix + "1"),
            Request(commonPrefix + "2"),
        };

        var rows = ByRefererAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(2, row.Hits);
        Assert.Equal(commonPrefix, row.Referer);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByRefererAggregator.Aggregate(null!, _targetDate));
    }

    private static NormalizedLogRequest Request(string referer, int timeTakenMilliseconds = 0, DateOnly? localDate = null) =>
        new()
        {
            UtcDateTime = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            LocalDateTime = new DateTime(2026, 5, 1, 12, 0, 0),
            LocalDate = localDate ?? _targetDate,
            UriStem = "/a",
            UserAgent = "-",
            Referer = referer,
            Status = 200,
            TimeTakenMilliseconds = timeTakenMilliseconds,
            ForwardedFor = "-",
        };
}
