using System.Net;

namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Builds a table cell for a table drawn with <see cref="TableCellContent.Html"/> (Google Charts'
/// <c>allowHtml</c>, which interprets every plain string cell as markup) that displays its text literally while
/// still sorting on the raw value. Any cell text taken from a request URI is attacker-influenced, so it must
/// go through here rather than being added to such a table as a bare string.
/// </summary>
internal static class HtmlSafeTableCell
{
    /// <summary>
    /// Creates a <c>{ v, f }</c> cell (Google Charts' formatted-value shape): <c>v</c> is the raw text, which the
    /// table sorts on, and <c>f</c> is its HTML-encoded form, which the table displays.
    /// </summary>
    /// <param name="text">The cell text; <see langword="null"/> renders as blank.</param>
    /// <returns>The cell.</returns>
    public static IReadOnlyDictionary<string, string> Create(string? text)
    {
        var raw = text ?? string.Empty;

        return new Dictionary<string, string> { ["v"] = raw, ["f"] = WebUtility.HtmlEncode(raw) };
    }
}
