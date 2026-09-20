using System.Globalization;
using IisLogParserArcGIS.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace IisLogParserArcGIS.Logging;

/// <summary>
/// Builds the program's <see cref="ILoggerFactory"/> from Serilog, writing to both the console and a rolling
/// file under the configured log directory.
/// </summary>
public static class SerilogLoggerFactoryBuilder
{
    private const string MicrosoftSourceContext = "Microsoft";
    private const string LogFileName = "iislogparser.log";
    private const long FileSizeLimitBytes = 10L * 1024 * 1024;
    private const int RetainedFileCountLimit = 10;
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// The least verbose the global minimum level is ever allowed to be, regardless of the configured
    /// <see cref="AppSettings.LogLevel"/>. Guarantees baseline operational messages - such as the startup
    /// confirmation line - are never silently dropped even if an operator configures a quieter level.
    /// </summary>
    private const LogEventLevel MaximumEffectiveLevel = LogEventLevel.Warning;

    /// <summary>
    /// Creates the logger factory, resolving a relative <see cref="AppSettings.LogOutputDirectory"/> against
    /// <paramref name="baseDirectory"/> and creating it if it doesn't already exist.
    /// </summary>
    /// <param name="settings">The bound application settings.</param>
    /// <param name="baseDirectory">The directory a relative log output directory is resolved against.</param>
    /// <returns>The created logger factory. Disposing it flushes and closes the underlying Serilog sinks.</returns>
    public static ILoggerFactory Create(AppSettings settings, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(baseDirectory);

        var configuredLevel = LogEventLevelResolver.Resolve(settings.LogLevel, LogEventLevel.Warning);
        var applicationLevel = (LogEventLevel)Math.Min((int)configuredLevel, (int)MaximumEffectiveLevel);
        var logDirectory = RelativePathResolver.Resolve(settings.LogOutputDirectory, baseDirectory);

        Directory.CreateDirectory(logDirectory);

#pragma warning disable CC0098 // LoggerConfiguration fluent API; chaining is the intended usage.
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Is(applicationLevel)
            .MinimumLevel.Override(MicrosoftSourceContext, LogEventLevel.Error)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: OutputTemplate, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                Path.Combine(logDirectory, LogFileName),
                rollingInterval: RollingInterval.Infinite,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: FileSizeLimitBytes,
                retainedFileCountLimit: RetainedFileCountLimit,
                outputTemplate: OutputTemplate,
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
#pragma warning restore CC0098

        return new SerilogLoggerFactory(serilogLogger, dispose: true);
    }
}
