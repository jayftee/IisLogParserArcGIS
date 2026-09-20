using System.Globalization;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// Independently counts a UTC calendar date's raw data lines straight off disk - the same file-naming
/// convention <c>LogFileLocator</c> globs on, and the same header rules <c>LogFileParser</c> follows (a header
/// block missing a <see cref="RequiredLogFieldNames"/> skips its whole physical file; a data line's field count
/// must match its governing header; a request is grouped under the raw <c>date</c> field, resolved by name) -
/// but computed here without going through any parsing code, so tests have an oracle for the shipped
/// executable's reported counts that isn't derived from the implementation under test. Only valid for the
/// shipped, UTC-only configuration, where a request's <c>local_date</c> grouping key is exactly its raw
/// <c>date</c> field.
/// </summary>
internal static class RawCorpusLineCounter
{
    private const string UtcDateFormat = "yyMMdd";
    private const string IsoDateFormat = "yyyy-MM-dd";
    private const string HeaderPrefix = "#Fields:";

    /// <summary>
    /// Counts <paramref name="utcDate"/>'s data lines across every physical file matching it in
    /// <paramref name="corpusDirectory"/>.
    /// </summary>
    /// <param name="corpusDirectory">The directory containing the IIS W3C log files.</param>
    /// <param name="utcDate">The UTC calendar date to count.</param>
    /// <returns>The independently-computed counts for <paramref name="utcDate"/>.</returns>
    public static CorpusDateCounts Count(string corpusDirectory, DateOnly utcDate)
    {
        ArgumentNullException.ThrowIfNull(corpusDirectory);

        var searchPattern = $"u_ex{utcDate.ToString(UtcDateFormat, CultureInfo.InvariantCulture)}*.log";
        var files = Directory.GetFiles(corpusDirectory, searchPattern, SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            throw new FileNotFoundException($"No corpus files matched '{searchPattern}' in '{corpusDirectory}'.");
        }

        var isoDate = utcDate.ToString(IsoDateFormat, CultureInfo.InvariantCulture);
        var total = 0;
        var matchingDate = 0;

        foreach (var file in files)
        {
            var (fileTotal, fileMatchingDate) = CountFile(file, isoDate);
            total += fileTotal;
            matchingDate += fileMatchingDate;
        }

        return new CorpusDateCounts { Total = total, MatchingDate = matchingDate };
    }

    private static (int Total, int MatchingDate) CountFile(string filePath, string isoDate)
    {
        var total = 0;
        var matchingDate = 0;
        string[]? fieldNames = null;
        var dateColumnIndex = -1;
        var fileIsSkipped = false;

        foreach (var line in File.ReadLines(filePath))
        {
            if (line.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                fieldNames = line[HeaderPrefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                dateColumnIndex = Array.FindIndex(fieldNames, name => string.Equals(name, RequiredLogFieldNames.Date, StringComparison.OrdinalIgnoreCase));
                fileIsSkipped |= RequiredLogFieldNames.All.Any(required => Array.Exists(fieldNames, name => string.Equals(name, required, StringComparison.OrdinalIgnoreCase)) is false);
                continue;
            }

            if (IsDataLine(line) is false || fieldNames is null)
            {
                continue;
            }

            total++;

            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (dateColumnIndex >= 0 && fields.Length == fieldNames.Length && string.Equals(fields[dateColumnIndex], isoDate, StringComparison.Ordinal))
            {
                matchingDate++;
            }
        }

        return fileIsSkipped || fieldNames is null ? (0, 0) : (total, matchingDate);
    }

    private static bool IsDataLine(string line) => string.IsNullOrWhiteSpace(line) is false && line.StartsWith('#') is false;
}
