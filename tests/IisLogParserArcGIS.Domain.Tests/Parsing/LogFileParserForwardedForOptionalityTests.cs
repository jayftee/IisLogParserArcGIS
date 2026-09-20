using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Parsing;

/// <summary>
/// Integration coverage proving that a header block lacking <c>X-Forwarded-For</c> does not disturb any of the
/// other six aggregates, and that the by-forwarded-for-IP aggregate correctly reflects a same-day mix of files
/// where only some declare the field (ticket 16).
/// </summary>
public class LogFileParserForwardedForOptionalityTests
{
    private const string HeaderWithoutForwardedFor =
        "#Fields: date time cs-uri-stem cs(User-Agent) cs(Referer) sc-status time-taken";

    private const string HeaderWithForwardedFor =
        "#Fields: date time cs-uri-stem cs(User-Agent) cs(Referer) sc-status time-taken X-Forwarded-For";

    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Parse_WithHeaderLackingForwardedFor_FullyProcessesFileAndPopulatesEverySixOtherAggregate()
    {
        var lines = new[]
        {
            HeaderWithoutForwardedFor,
            "2026-05-01 12:00:00 /mimas/rest/services/foo/MapServer/export Mozilla/5.0 http://example.com 200 150",
        };
        var parser = new LogFileParser();

        var result = parser.Parse("u_ex260501.log", lines, TimeSpan.Zero);

        Assert.False(result.WasSkipped);
        Assert.Empty(ByForwardedForIpAggregator.Aggregate(result.Requests, _targetDate));
        Assert.NotEmpty(ByRootAggregator.Aggregate(result.Requests, _targetDate));
        Assert.NotEmpty(ByUriAggregator.Aggregate(result.Requests, _targetDate));
        Assert.NotEmpty(ByUserAgentAggregator.Aggregate(result.Requests, _targetDate));
        Assert.NotEmpty(ByRefererAggregator.Aggregate(result.Requests, _targetDate));
        Assert.NotEmpty(ByRefererAndUriAggregator.Aggregate(result.Requests, _targetDate));
        Assert.NotEmpty(ByArcGisServiceAggregator.Aggregate(result.Requests, _targetDate));
    }

    [Fact]
    public void Parse_WithMixedSameDayFilesWhereOnlyOneDeclaresForwardedFor_AggregatesOnlyTheDeclaringLinesForwardedForIp()
    {
        var fileWithoutForwardedFor = new[]
        {
            HeaderWithoutForwardedFor,
            "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10",
        };
        var fileWithForwardedFor = new[]
        {
            HeaderWithForwardedFor,
            "2026-05-01 13:00:00 /b Mozilla/5.0 - 200 20 203.0.113.1",
        };
        var parser = new LogFileParser();

        var resultWithout = parser.Parse("u_ex260501_x.log", fileWithoutForwardedFor, TimeSpan.Zero);
        var resultWith = parser.Parse("u_ex260501_x_10359.log", fileWithForwardedFor, TimeSpan.Zero);
        var allRequests = resultWithout.Requests.Concat(resultWith.Requests).ToArray();

        Assert.False(resultWithout.WasSkipped);
        Assert.False(resultWith.WasSkipped);

        var forwardedForRows = ByForwardedForIpAggregator.Aggregate(allRequests, _targetDate);
        var row = Assert.Single(forwardedForRows);
        Assert.Equal("203.0.113.1", row.ForwardedForIp);
        Assert.Equal(1, row.Hits);

        var uriRows = ByUriAggregator.Aggregate(allRequests, _targetDate);
        Assert.Equal(2, uriRows.Count);
    }
}
