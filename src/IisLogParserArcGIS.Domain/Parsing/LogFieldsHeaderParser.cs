namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// Recognizes and parses a raw IIS <c>#Fields</c> header line into a <see cref="LogFieldIndex"/>.
/// </summary>
public static class LogFieldsHeaderParser
{
    private const string HeaderPrefix = "#Fields:";

    /// <summary>
    /// Attempts to parse <paramref name="line"/> as a <c>#Fields</c> header line.
    /// </summary>
    /// <param name="line">The raw line to inspect.</param>
    /// <param name="fieldIndex">The parsed field index, when <paramref name="line"/> is a header line.</param>
    /// <returns><see langword="true"/> when <paramref name="line"/> is a <c>#Fields</c> header line.</returns>
    public static bool TryParse(string line, out LogFieldIndex? fieldIndex)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (line.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var fieldNames = line[HeaderPrefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            fieldIndex = new LogFieldIndex(fieldNames);
            return true;
        }

        fieldIndex = null;
        return false;
    }
}
