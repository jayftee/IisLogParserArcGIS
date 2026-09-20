using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByRootAggregatorTests
{
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithMultipleUrisUnderTheSameRoot_SumsHitsAndTimeTakenSeconds()
    {
        var requests = new[]
        {
            Request("/arcgis/rest/services/a", timeTakenMilliseconds: 1000),
            Request("/arcgis/rest/services/b", timeTakenMilliseconds: 2000),
            Request("/portal/home", timeTakenMilliseconds: 500),
        };

        var rows = ByRootAggregator.Aggregate(requests, _targetDate);

        var rowForArcgis = Assert.Single(rows, row => row.Root == "arcgis");
        Assert.Equal(2, rowForArcgis.Hits);
        Assert.Equal(3.0, rowForArcgis.TimeTakenSecond);
        var rowForPortal = Assert.Single(rows, row => row.Root == "portal");
        Assert.Equal(1, rowForPortal.Hits);
        Assert.Equal(0.5, rowForPortal.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates()
    {
        var requests = new[]
        {
            Request("/arcgis/a", localDate: _targetDate),
            Request("/arcgis/a", localDate: _targetDate.AddDays(1)),
        };

        var rows = ByRootAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
    }

    [Fact]
    public void Aggregate_WithRequestsLackingAPathSegment_GroupsUnderTheFallbackRoot()
    {
        var requests = new[] { Request("/"), Request(string.Empty) };

        var rows = ByRootAggregator.Aggregate(requests, _targetDate);

        var row = Assert.Single(rows);
        Assert.Equal("-", row.Root);
        Assert.Equal(2, row.Hits);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByRootAggregator.Aggregate(null!, _targetDate));
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
