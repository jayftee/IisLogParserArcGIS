using System.Globalization;

namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// Parses one raw IIS log data line, governed by a <see cref="LogFieldIndex"/>, into a <see cref="NormalizedLogRequest"/>.
/// </summary>
public static class LogLineParser
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Attempts to parse <paramref name="line"/> into a <see cref="NormalizedLogRequest"/>.
    /// </summary>
    /// <param name="line">The raw data line, already known not to be a comment or header line.</param>
    /// <param name="fieldIndex">The field index resolved from the line's governing header block.</param>
    /// <param name="localUtcOffset">The configured local time zone's UTC offset.</param>
    /// <returns>The parse outcome.</returns>
    public static LogLineParseOutcome Parse(string line, LogFieldIndex fieldIndex, TimeSpan localUtcOffset)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(fieldIndex);

        var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length != fieldIndex.FieldCount)
        {
            return LogLineParseOutcome.Failure($"field count mismatch (expected {fieldIndex.FieldCount}, got {fields.Length})");
        }

        if (TryParseUtcDateTime(fields, fieldIndex, out var utcDateTime) is false)
        {
            return LogLineParseOutcome.Failure(
                $"unparseable date/time '{fieldIndex.GetValue(fields, RequiredLogFieldNames.Date)} {fieldIndex.GetValue(fields, RequiredLogFieldNames.Time)}'");
        }

        var statusText = fieldIndex.GetValue(fields, RequiredLogFieldNames.Status);

        if (TryParseInt(statusText, out var status) is false)
        {
            return LogLineParseOutcome.Failure($"unparseable status '{statusText}'");
        }

        var timeTakenText = fieldIndex.GetValue(fields, RequiredLogFieldNames.TimeTaken);

        if (TryParseInt(timeTakenText, out var timeTakenMilliseconds) is false)
        {
            return LogLineParseOutcome.Failure($"unparseable time-taken '{timeTakenText}'");
        }

        var localDateTime = DateTime.SpecifyKind(utcDateTime + localUtcOffset, DateTimeKind.Unspecified);

        return LogLineParseOutcome.Success(new NormalizedLogRequest
        {
            UtcDateTime = utcDateTime,
            LocalDateTime = localDateTime,
            LocalDate = DateOnly.FromDateTime(localDateTime),
            UriStem = fieldIndex.GetValue(fields, RequiredLogFieldNames.UriStem),
            UserAgent = fieldIndex.GetValue(fields, RequiredLogFieldNames.UserAgent),
            Referer = fieldIndex.GetValue(fields, RequiredLogFieldNames.Referer),
            Status = status,
            TimeTakenMilliseconds = timeTakenMilliseconds,
            ForwardedFor = fieldIndex.HasField(RequiredLogFieldNames.ForwardedFor)
                ? fieldIndex.GetValue(fields, RequiredLogFieldNames.ForwardedFor)
                : null,
        });
    }

    private static bool TryParseUtcDateTime(string[] fields, LogFieldIndex fieldIndex, out DateTime utcDateTime)
    {
        var dateText = fieldIndex.GetValue(fields, RequiredLogFieldNames.Date);
        var timeText = fieldIndex.GetValue(fields, RequiredLogFieldNames.Time);

        if (DateTime.TryParseExact($"{dateText} {timeText}", DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            utcDateTime = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }

        utcDateTime = default;
        return false;
    }

    private static bool TryParseInt(string text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
