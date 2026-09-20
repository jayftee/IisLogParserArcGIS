using IisLogParserArcGIS.Cli;
using IisLogParserArcGIS.Logging;
using Microsoft.Extensions.Logging;

namespace IisLogParserArcGIS.Tests.Logging;

public class StartupAnnouncerTests
{
    [Fact]
    public void AnnounceStartup_LogsWarningWithArgumentDetails()
    {
        using var provider = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        var announcer = new StartupAnnouncer(loggerFactory);
        var arguments = new ParsedArguments("C:\\logs", new DateOnly(2026, 5, 1), "C:\\out.db");

        announcer.AnnounceStartup(arguments);

        var entry = Assert.Single(provider.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("C:\\logs", entry.Message, StringComparison.Ordinal);
        Assert.Contains("C:\\out.db", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnnounceStartup_WithoutLoggerFactory_DoesNotThrow()
    {
        var announcer = new StartupAnnouncer();
        var arguments = new ParsedArguments("logs", new DateOnly(2026, 5, 1), "out.db");

        announcer.AnnounceStartup(arguments);
    }

    [Fact]
    public void AnnounceStartup_WithNullArguments_ThrowsArgumentNullException()
    {
        var announcer = new StartupAnnouncer();

        Assert.Throws<ArgumentNullException>(() => announcer.AnnounceStartup(null!));
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
