using IisLogParserArcGIS.Cli;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IisLogParserArcGIS.Logging;

/// <summary>
/// Logs the startup confirmation line an operator sees once the program's arguments have validated successfully.
/// </summary>
public sealed class StartupAnnouncer
{
    private readonly ILogger<StartupAnnouncer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupAnnouncer"/> class.
    /// </summary>
    /// <param name="loggerFactory">
    /// The factory to create the logger from. When omitted, logging is a no-op (see ADR 0003).
    /// </param>
    public StartupAnnouncer(ILoggerFactory? loggerFactory = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<StartupAnnouncer>();
    }

    /// <summary>
    /// Logs the startup confirmation line for a validated run. Logged at <see cref="LogLevel.Warning"/> - the
    /// application's default minimum level - so it's always visible regardless of the configured log level.
    /// </summary>
    /// <param name="arguments">The validated command-line arguments for this run.</param>
    public void AnnounceStartup(ParsedArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        _logger.LogWarning(
            "Starting IisLogParserArcGIS: source directory={LogSourceDirectory}, target local date={TargetLocalDate}, output database={OutputDatabasePath}",
            arguments.LogSourceDirectory,
            arguments.TargetLocalDate,
            arguments.OutputDatabasePath);
    }
}
