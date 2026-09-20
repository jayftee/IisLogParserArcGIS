using Serilog.Events;

namespace IisLogParserArcGIS.Logging;

/// <summary>
/// Resolves the configured application log level into a <see cref="LogEventLevel"/>.
/// </summary>
public static class LogEventLevelResolver
{
    /// <summary>
    /// Parses the configured log level, by name, falling back to <paramref name="fallback"/> when blank or
    /// unrecognized.
    /// </summary>
    /// <param name="configuredLevel">The configured level name (e.g. <c>Warning</c>, <c>Debug</c>).</param>
    /// <param name="fallback">The level to use when <paramref name="configuredLevel"/> is blank or invalid.</param>
    /// <returns>The resolved level.</returns>
    public static LogEventLevel Resolve(string? configuredLevel, LogEventLevel fallback)
    {
        if (string.IsNullOrWhiteSpace(configuredLevel))
        {
            return fallback;
        }

        var parsed = Enum.TryParse<LogEventLevel>(configuredLevel, ignoreCase: true, out var level) && Enum.IsDefined(level);
        return parsed ? level : fallback;
    }
}
