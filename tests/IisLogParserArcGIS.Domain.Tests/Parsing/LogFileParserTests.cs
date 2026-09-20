using IisLogParserArcGIS.Domain.Parsing;
using Microsoft.Extensions.Logging;

namespace IisLogParserArcGIS.Domain.Tests.Parsing;

public class LogFileParserTests
{
    private const string StandardHeader = "#Fields: date time cs-uri-stem cs(User-Agent) cs(Referer) sc-status time-taken X-Forwarded-For";

    [Fact]
    public void Parse_WithValidLinesAndOneMalformedLine_SkipsOnlyTheMalformedLine()
    {
        var lines = new[]
        {
            StandardHeader,
            "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -",
            "2026-05-01 12:00:01 /b Mozilla/5.0 - 200 20 -",
            "2026-05-01 12:00:02 /c 200 30 -",
        };
        var parser = new LogFileParser();

        var result = parser.Parse("u_ex260501_x_1.log", lines, TimeSpan.Zero);

        Assert.False(result.WasSkipped);
        Assert.Equal(2, result.Requests.Count);
        Assert.Equal(3, result.Counts.Total);
        Assert.Equal(2, result.Counts.Valid);
        Assert.Equal(1, result.Counts.Invalid);
    }

    [Fact]
    public void Parse_WithHeaderMissingRequiredField_SkipsWholeFile()
    {
        var lines = new[]
        {
            "#Fields: date time cs-uri-stem cs(User-Agent) sc-status time-taken X-Forwarded-For",
            "2026-05-01 12:00:00 /a Mozilla/5.0 200 10 -",
        };
        var parser = new LogFileParser();

        var result = parser.Parse("u_ex260501_x_1.log", lines, TimeSpan.Zero);

        Assert.True(result.WasSkipped);
        Assert.Empty(result.Requests);
        Assert.Equal(0, result.Counts.Total);
        Assert.Contains("cs(Referer)", result.SkipReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_WithHeaderMissingOnlyForwardedFor_ParsesFileNormallyWithNullForwardedFor()
    {
        var lines = new[]
        {
            "#Fields: date time cs-uri-stem cs(User-Agent) cs(Referer) sc-status time-taken",
            "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10",
        };
        var parser = new LogFileParser();

        var result = parser.Parse("u_ex260501.log", lines, TimeSpan.Zero);

        Assert.False(result.WasSkipped);
        var request = Assert.Single(result.Requests);
        Assert.Equal("/a", request.UriStem);
        Assert.Null(request.ForwardedFor);
    }

    [Fact]
    public void Parse_WithNoHeaderBlockAtAll_SkipsWholeFile()
    {
        var lines = new[] { "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -" };
        var parser = new LogFileParser();

        var result = parser.Parse("u_ex260501_x_1.log", lines, TimeSpan.Zero);

        Assert.True(result.WasSkipped);
        Assert.Contains("no #Fields header block found", result.SkipReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_WithMultipleHeaderBlocksInDifferentOrder_ResolvesEachByName()
    {
        var lines = new[]
        {
            StandardHeader,
            "2026-05-01 01:00:00 /a Mozilla/5.0 - 200 10 -",
            "#Fields: X-Forwarded-For date time sc-status time-taken cs-uri-stem cs(User-Agent) cs(Referer)",
            "192.168.1.1 2026-05-01 02:00:00 200 20 /b Mozilla/5.0 -",
        };
        var parser = new LogFileParser();

        var result = parser.Parse("u_ex260501_x_1.log", lines, TimeSpan.Zero);

        Assert.False(result.WasSkipped);
        Assert.Equal(2, result.Requests.Count);
        Assert.Equal("/a", result.Requests[0].UriStem);
        Assert.Equal("/b", result.Requests[1].UriStem);
    }

    [Fact]
    public void Parse_WithDataLineBeforeAnyHeader_SkipsThatLineOnlyAndBecauseAHeaderExistsElsewhereTheFileIsNotSkipped()
    {
        var lines = new[]
        {
            "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -",
            StandardHeader,
            "2026-05-01 12:00:01 /b Mozilla/5.0 - 200 20 -",
        };
        var parser = new LogFileParser();

        var result = parser.Parse("u_ex260501_x_1.log", lines, TimeSpan.Zero);

        Assert.False(result.WasSkipped);
        Assert.Equal(2, result.Counts.Total);
        Assert.Equal(1, result.Counts.Valid);
        Assert.Equal(1, result.Counts.Invalid);
        Assert.Equal("/b", Assert.Single(result.Requests).UriStem);
    }

    [Fact]
    public void Parse_IgnoresBlankAndCommentLines()
    {
        var lines = new[]
        {
            "#Software: Microsoft Internet Information Services 10.0",
            string.Empty,
            StandardHeader,
            "   ",
            "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -",
        };
        var parser = new LogFileParser();

        var result = parser.Parse("u_ex260501_x_1.log", lines, TimeSpan.Zero);

        Assert.False(result.WasSkipped);
        Assert.Equal(1, result.Counts.Total);
        Assert.Equal(1, result.Counts.Valid);
    }

    [Fact]
    public void Parse_WhenSkippingAMalformedLine_LogsWarningWithFileNameAndLineNumberCorrelationId()
    {
        var lines = new[]
        {
            StandardHeader,
            "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -",
            "malformed line",
        };
        using var loggerFactory = new CapturingLoggerFactory();
        var parser = new LogFileParser(loggerFactory);

        parser.Parse("u_ex260501_x_1.log", lines, TimeSpan.Zero);

        Assert.Contains(loggerFactory.Entries, entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("u_ex260501_x_1.log:3", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_WhenSkippingWholeFile_LogsErrorWithFileNameCorrelationId()
    {
        var lines = new[] { "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -" };
        using var loggerFactory = new CapturingLoggerFactory();
        var parser = new LogFileParser(loggerFactory);

        parser.Parse("u_ex260501_x_1.log", lines, TimeSpan.Zero);

        Assert.Contains(loggerFactory.Entries, entry =>
            entry.Level == LogLevel.Error && entry.Message.Contains("u_ex260501_x_1.log", StringComparison.Ordinal));
    }

    private sealed class CapturingLoggerFactory : ILoggerFactory
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly CapturingLoggerFactory _factory;

            public CapturingLogger(CapturingLoggerFactory factory)
            {
                _factory = factory;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

#pragma warning disable CC0042 // Required ILogger.Log<TState> interface signature; cannot be reduced.
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
#pragma warning restore CC0042
            {
                _factory.Entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
