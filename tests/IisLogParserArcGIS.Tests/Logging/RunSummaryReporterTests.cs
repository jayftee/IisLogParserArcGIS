using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;
using IisLogParserArcGIS.Logging;
using Microsoft.Extensions.Logging;

namespace IisLogParserArcGIS.Tests.Logging;

public class RunSummaryReporterTests
{
    [Fact]
    public void Report_LogsWarningWithCountsAndElapsedTime()
    {
        using var provider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var reporter = new RunSummaryReporter(loggerFactory);
        var counts = new LogLineCounts(Total: 10, Valid: 8, Invalid: 2);

        reporter.Report(counts, TimeSpan.FromSeconds(1.5));

        var entry = Assert.Single(provider.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("10", entry.Message, StringComparison.Ordinal);
        Assert.Contains("8", entry.Message, StringComparison.Ordinal);
        Assert.Contains("2", entry.Message, StringComparison.Ordinal);
        Assert.Contains("1.50", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportFieldMapsAttribution_LogsWarningWithEachAttributionCount()
    {
        using var provider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var reporter = new RunSummaryReporter(loggerFactory);

        reporter.ReportFieldMapsAttribution(new DeviceAttributionCounts(AttributedSameDay: 11, AttributedByBackfill: 22, Unattributed: 33));

        var entry = Assert.Single(provider.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("66", entry.Message, StringComparison.Ordinal);
        Assert.Contains("11", entry.Message, StringComparison.Ordinal);
        Assert.Contains("22", entry.Message, StringComparison.Ordinal);
        Assert.Contains("33", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportByRefererAndUriSkipped_LogsWarningNamingTheSetting()
    {
        using var provider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var reporter = new RunSummaryReporter(loggerFactory);

        reporter.ReportByRefererAndUriSkipped();

        var entry = Assert.Single(provider.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("ComputeByRefererAndUri", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportFieldMapsAttribution_WithNullCounts_ThrowsArgumentNullException()
    {
        var reporter = new RunSummaryReporter();

        Assert.Throws<ArgumentNullException>(() => reporter.ReportFieldMapsAttribution(null!));
    }

    [Fact]
    public void ReportSurvey123Attribution_LogsWarningWithEachAttributionCount()
    {
        using var provider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var reporter = new RunSummaryReporter(loggerFactory);

        reporter.ReportSurvey123Attribution(new DeviceAttributionCounts(AttributedSameDay: 11, AttributedByBackfill: 22, Unattributed: 33));

        var entry = Assert.Single(provider.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.StartsWith("Survey123 attribution:", entry.Message, StringComparison.Ordinal);
        Assert.Contains("66", entry.Message, StringComparison.Ordinal);
        Assert.Contains("11", entry.Message, StringComparison.Ordinal);
        Assert.Contains("22", entry.Message, StringComparison.Ordinal);
        Assert.Contains("33", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportSurvey123Attribution_WithNullCounts_ThrowsArgumentNullException()
    {
        var reporter = new RunSummaryReporter();

        Assert.Throws<ArgumentNullException>(() => reporter.ReportSurvey123Attribution(null!));
    }

    [Fact]
    public void Report_WithoutLoggerFactory_DoesNotThrow()
    {
        var reporter = new RunSummaryReporter();

        reporter.Report(LogLineCounts.Zero, TimeSpan.Zero);
    }

    [Fact]
    public void Report_WithNullCounts_ThrowsArgumentNullException()
    {
        var reporter = new RunSummaryReporter();

        Assert.Throws<ArgumentNullException>(() => reporter.Report(null!, TimeSpan.Zero));
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly CapturingLoggerProvider _provider;

            public CapturingLogger(CapturingLoggerProvider provider)
            {
                _provider = provider;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

#pragma warning disable CC0042 // Required ILogger.Log<TState> interface signature; cannot be reduced.
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
#pragma warning restore CC0042
            {
                _provider.Entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
