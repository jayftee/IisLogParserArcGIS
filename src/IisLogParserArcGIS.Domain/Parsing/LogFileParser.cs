using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// Parses one physical log file's raw lines into normalized requests, tracking the most recently seen
/// <c>#Fields</c> header block (a file may contain several, e.g. after an IIS logging restart mid-day) and
/// resolving every field by name from its governing header rather than by a fixed position.
/// </summary>
public sealed class LogFileParser
{
    private readonly ILogger<LogFileParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogFileParser"/> class.
    /// </summary>
    /// <param name="loggerFactory">
    /// The factory to create the logger from. When omitted, logging is a no-op (see ADR 0003).
    /// </param>
    public LogFileParser(ILoggerFactory? loggerFactory = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<LogFileParser>();
    }

    /// <summary>
    /// Parses <paramref name="lines"/> - one physical file's raw content - into normalized requests. If any
    /// header block in the file is missing a field required by aggregation (or no header block is present at
    /// all), the entire file is skipped rather than partially processed.
    /// </summary>
    /// <param name="fileName">The file name, used only to build each log statement's correlation ID.</param>
    /// <param name="lines">The file's raw lines, in order.</param>
    /// <param name="localUtcOffset">The configured local time zone's UTC offset.</param>
    /// <returns>The parse result.</returns>
    public LogFileParseResult Parse(string fileName, IReadOnlyList<string> lines, TimeSpan localUtcOffset)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(lines);

        var skipReason = FindSkipReason(lines);

        if (skipReason is not null)
        {
            _logger.LogError("{CorrelationId}: Skipping file - {Reason}.", fileName, skipReason);
            return LogFileParseResult.Skipped(skipReason);
        }

        return ParseDataLines(fileName, lines, localUtcOffset);
    }

    private static string? FindSkipReason(IReadOnlyList<string> lines)
    {
        var sawHeader = false;

        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            if (LogFieldsHeaderParser.TryParse(lines[lineIndex], out var fieldIndex) is false)
            {
                continue;
            }

            sawHeader = true;
            var missingFields = RequiredLogFieldNames.All.Where(name => fieldIndex!.HasField(name) is false).ToArray();

            if (missingFields.Length > 0)
            {
                return $"header block at line {lineIndex + 1} is missing required field(s): {string.Join(", ", missingFields)}";
            }
        }

        return sawHeader ? null : "no #Fields header block found";
    }

    private LogFileParseResult ParseDataLines(string fileName, IReadOnlyList<string> lines, TimeSpan localUtcOffset)
    {
        NormalizedLogRequest? TryParseDataLine(int lineNumber, string line, LogFieldIndex? fieldIndex)
        {
            var correlationId = $"{fileName}:{lineNumber}";

            if (fieldIndex is null)
            {
                _logger.LogWarning("{CorrelationId}: Skipping line - no header block seen yet.", correlationId);
                return null;
            }

            var outcome = LogLineParser.Parse(line, fieldIndex, localUtcOffset);

            if (outcome.Succeeded)
            {
                return outcome.Request;
            }

            _logger.LogWarning("{CorrelationId}: Skipping line - {Reason}.", correlationId, outcome.FailureReason);
            return null;
        }

        var requests = new List<NormalizedLogRequest>();
        LogFieldIndex? currentFieldIndex = null;
        var validLines = 0;
        var totalLines = 0;

        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (LogFieldsHeaderParser.TryParse(line, out var headerFieldIndex))
            {
                currentFieldIndex = headerFieldIndex;
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            totalLines++;

            if (TryParseDataLine(lineIndex + 1, line, currentFieldIndex) is { } request)
            {
                requests.Add(request);
                validLines++;
            }
        }

        return LogFileParseResult.Parsed(requests, new LogLineCounts(totalLines, validLines, totalLines - validLines));
    }
}
