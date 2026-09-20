using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Parsing;

public class LogLineParserTests
{
    private const string ValidLine =
        "2026-05-01 12:00:00 /mimas/rest/services/foo/MapServer/export Mozilla/5.0 http://example.com 200 150 192.168.1.1";

    private static readonly LogFieldIndex _columns = new(
        ["date", "time", "cs-uri-stem", "cs(User-Agent)", "cs(Referer)", "sc-status", "time-taken", "X-Forwarded-For"]);

    [Fact]
    public void Parse_WithValidLine_ReturnsSuccessWithNormalizedFields()
    {
        var outcome = LogLineParser.Parse(ValidLine, _columns, TimeSpan.Zero);

        Assert.True(outcome.Succeeded);
        var request = outcome.Request!;
        Assert.Equal(new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc), request.UtcDateTime);
        Assert.Equal("/mimas/rest/services/foo/MapServer/export", request.UriStem);
        Assert.Equal("Mozilla/5.0", request.UserAgent);
        Assert.Equal("http://example.com", request.Referer);
        Assert.Equal(200, request.Status);
        Assert.Equal(150, request.TimeTakenMilliseconds);
        Assert.Equal("192.168.1.1", request.ForwardedFor);
    }

    [Fact]
    public void Parse_WithNegativeOffset_DerivesLocalDateTimeAndLocalDate()
    {
        var outcome = LogLineParser.Parse(ValidLine, _columns, TimeSpan.FromHours(-5));

        var request = outcome.Request!;
        Assert.Equal(new DateTime(2026, 5, 1, 7, 0, 0), request.LocalDateTime);
        Assert.Equal(new DateOnly(2026, 5, 1), request.LocalDate);
    }

    [Fact]
    public void Parse_WithOffsetCrossingMidnight_RollsLocalDateBack()
    {
        const string LineNearMidnight =
            "2026-05-01 02:00:00 /mimas/rest/services/foo/MapServer/export Mozilla/5.0 http://example.com 200 150 192.168.1.1";

        var outcome = LogLineParser.Parse(LineNearMidnight, _columns, TimeSpan.FromHours(-5));

        Assert.Equal(new DateOnly(2026, 4, 30), outcome.Request!.LocalDate);
    }

    [Fact]
    public void Parse_WithGoverningHeaderNotDeclaringForwardedFor_ReturnsNullForwardedFor()
    {
        var columnsWithoutForwardedFor = new LogFieldIndex(
            ["date", "time", "cs-uri-stem", "cs(User-Agent)", "cs(Referer)", "sc-status", "time-taken"]);
        const string LineWithoutForwardedFor =
            "2026-05-01 12:00:00 /mimas/rest/services/foo/MapServer/export Mozilla/5.0 http://example.com 200 150";

        var outcome = LogLineParser.Parse(LineWithoutForwardedFor, columnsWithoutForwardedFor, TimeSpan.Zero);

        Assert.True(outcome.Succeeded);
        Assert.Null(outcome.Request!.ForwardedFor);
    }

    [Fact]
    public void Parse_WithFieldCountMismatch_ReturnsFailure()
    {
        const string ShortLine = "2026-05-01 12:00:00 /mimas/rest 200 150 192.168.1.1";

        var outcome = LogLineParser.Parse(ShortLine, _columns, TimeSpan.Zero);

        Assert.False(outcome.Succeeded);
        Assert.Contains("field count mismatch", outcome.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_WithUnparseableDate_ReturnsFailure()
    {
        const string BadDateLine =
            "not-a-date 12:00:00 /mimas/rest/services/foo/MapServer/export Mozilla/5.0 http://example.com 200 150 192.168.1.1";

        var outcome = LogLineParser.Parse(BadDateLine, _columns, TimeSpan.Zero);

        Assert.False(outcome.Succeeded);
        Assert.Contains("unparseable date/time", outcome.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_WithUnparseableStatus_ReturnsFailure()
    {
        const string BadStatusLine =
            "2026-05-01 12:00:00 /mimas/rest/services/foo/MapServer/export Mozilla/5.0 http://example.com NOTASTATUS 150 192.168.1.1";

        var outcome = LogLineParser.Parse(BadStatusLine, _columns, TimeSpan.Zero);

        Assert.False(outcome.Succeeded);
        Assert.Contains("unparseable status", outcome.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_WithUnparseableTimeTaken_ReturnsFailure()
    {
        const string BadTimeTakenLine =
            "2026-05-01 12:00:00 /mimas/rest/services/foo/MapServer/export Mozilla/5.0 http://example.com 200 NOTANUMBER 192.168.1.1";

        var outcome = LogLineParser.Parse(BadTimeTakenLine, _columns, TimeSpan.Zero);

        Assert.False(outcome.Succeeded);
        Assert.Contains("unparseable time-taken", outcome.FailureReason, StringComparison.Ordinal);
    }
}
