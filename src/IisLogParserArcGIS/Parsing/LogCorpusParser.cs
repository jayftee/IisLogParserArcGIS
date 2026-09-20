using IisLogParserArcGIS.Domain.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IisLogParserArcGIS.Parsing;

/// <summary>
/// Reads every discovered physical log file from disk and delegates per-file parsing to the Domain's
/// <see cref="LogFileParser"/>, aggregating the results across the whole run.
/// </summary>
public sealed class LogCorpusParser
{
    private readonly ILogger<LogCorpusParser> _logger;
    private readonly LogFileParser _fileParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogCorpusParser"/> class.
    /// </summary>
    /// <param name="loggerFactory">
    /// The factory to create the logger from. When omitted, logging is a no-op (see ADR 0003).
    /// </param>
    public LogCorpusParser(ILoggerFactory? loggerFactory = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LogCorpusParser>();
        _fileParser = new LogFileParser(loggerFactory);
    }

    /// <summary>
    /// Parses every file in <paramref name="filePaths"/>.
    /// </summary>
    /// <param name="filePaths">The full paths of the discovered physical log files.</param>
    /// <param name="localUtcOffset">The configured local time zone's UTC offset.</param>
    /// <returns>The combined parse result.</returns>
    public LogCorpusParseResult Parse(IReadOnlyList<string> filePaths, TimeSpan localUtcOffset)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        var requests = new List<NormalizedLogRequest>();
        var counts = LogLineCounts.Zero;
        var skippedFileCount = 0;

        foreach (var filePath in filePaths)
        {
            if (TryReadLines(filePath, out var lines) is false)
            {
                skippedFileCount++;
                continue;
            }

            var result = _fileParser.Parse(Path.GetFileName(filePath), lines, localUtcOffset);

            if (result.WasSkipped)
            {
                skippedFileCount++;
                continue;
            }

            requests.AddRange(result.Requests);
            counts += result.Counts;
        }

        _logger.LogInformation(
            "Parsed {FileCount} file(s) ({SkippedFileCount} skipped): {TotalLines} line(s) read, {ValidLines} valid, {InvalidLines} invalid.",
            filePaths.Count,
            skippedFileCount,
            counts.Total,
            counts.Valid,
            counts.Invalid);

        return new LogCorpusParseResult { Requests = requests, Counts = counts, SkippedFileCount = skippedFileCount };
    }

    private bool TryReadLines(string filePath, out string[] lines)
    {
        try
        {
            lines = File.ReadAllLines(filePath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "{FileName}: Skipping file - could not be read: {Reason}.", Path.GetFileName(filePath), ex.Message);
            lines = [];
            return false;
        }
    }
}
