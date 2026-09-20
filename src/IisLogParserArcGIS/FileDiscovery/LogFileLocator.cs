using System.Globalization;
using IisLogParserArcGIS.Domain.FileDiscovery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IisLogParserArcGIS.FileDiscovery;

/// <summary>
/// Locates the physical IIS W3C log file(s) needed to fully cover a target local date. Everything after the UTC
/// date in a file name is treated as one opaque wildcard - it is never inspected, parsed, or branched on, since
/// the file's own <c>#Fields</c> header (not its name) is the only thing the program relies on for schema.
/// </summary>
public sealed class LogFileLocator
{
    private const string FileNamePrefix = "u_ex";
    private const string FileNameExtension = ".log";
    private const string UtcDateFormat = "yyMMdd";
    private const string LocalDateFormat = "yyyy-MM-dd";

    private readonly ILogger<LogFileLocator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogFileLocator"/> class.
    /// </summary>
    /// <param name="loggerFactory">
    /// The factory to create the logger from. When omitted, logging is a no-op (see ADR 0003).
    /// </param>
    public LogFileLocator(ILoggerFactory? loggerFactory = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LogFileLocator>();
    }

    /// <summary>
    /// Discovers every physical log file needed to fully cover <paramref name="targetLocalDate"/> in
    /// <paramref name="sourceDirectory"/>. Every UTC calendar date needed to cover the local day must have at
    /// least one matching file - a missing date is treated the same as no files at all, so that hits near local
    /// midnight can never be silently dropped by a partially-arrived day's files.
    /// </summary>
    /// <param name="sourceDirectory">The directory containing the IIS W3C log files.</param>
    /// <param name="targetLocalDate">The local calendar date being processed.</param>
    /// <param name="localUtcOffset">The configured local time zone's UTC offset on <paramref name="targetLocalDate"/>.</param>
    /// <returns>The full paths of every matching log file, ordered by file name.</returns>
    /// <exception cref="NoLogFilesFoundException">
    /// No log files matched one or more of the needed UTC date(s).
    /// </exception>
    public IReadOnlyList<string> Discover(string sourceDirectory, DateOnly targetLocalDate, TimeSpan localUtcOffset)
    {
        ArgumentNullException.ThrowIfNull(sourceDirectory);

        var requiredUtcDates = RequiredUtcDatesCalculator.Calculate(targetLocalDate, localUtcOffset);
        var sourceDirectoryExists = Directory.Exists(sourceDirectory);

        var filesByUtcDate = requiredUtcDates.ToDictionary(
            utcDate => utcDate,
            utcDate => sourceDirectoryExists ? FindFilesForUtcDate(sourceDirectory, utcDate) : []);

        var missingUtcDates = filesByUtcDate.Where(pair => pair.Value.Length == 0).Select(pair => pair.Key).ToArray();

        if (missingUtcDates.Length > 0)
        {
            var missingUtcDateText = string.Join(", ", missingUtcDates.Select(FormatUtcDate));
            var message = $"No log files found in '{sourceDirectory}' for UTC date(s) {missingUtcDateText} " +
                $"(local date {targetLocalDate.ToString(LocalDateFormat, CultureInfo.InvariantCulture)}).";

            _logger.LogError("{Message}", message);
            throw new NoLogFilesFoundException(message);
        }

        var matchedFiles = filesByUtcDate.Values
            .SelectMany(files => files)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        _logger.LogInformation(
            "Discovered {FileCount} log file(s) in '{SourceDirectory}' for local date {TargetLocalDate}: {Files}",
            matchedFiles.Length,
            sourceDirectory,
            targetLocalDate.ToString(LocalDateFormat, CultureInfo.InvariantCulture),
            string.Join(", ", matchedFiles));

        return matchedFiles;
    }

    private static string[] FindFilesForUtcDate(string sourceDirectory, DateOnly utcDate)
    {
        var searchPattern = $"{FileNamePrefix}{FormatUtcDate(utcDate)}*{FileNameExtension}";

        return Directory.GetFiles(sourceDirectory, searchPattern, SearchOption.TopDirectoryOnly)
            .Where(path => path.EndsWith(FileNameExtension, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string FormatUtcDate(DateOnly utcDate) => utcDate.ToString(UtcDateFormat, CultureInfo.InvariantCulture);
}
