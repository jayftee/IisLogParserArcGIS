using System.Globalization;
using System.Net;
using System.Text;

namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Writes one Complete View's table - the exact same <see cref="GoogleChartsRenderer.RenderTable"/>-shaped
/// <c>IReadOnlyList&lt;IReadOnlyList&lt;object?&gt;&gt;</c> each Complete View builder already assembles for its
/// HTML page - as a sibling <c>.tsv</c> file, and renders the "Download as TSV" link shown on that same page
/// (reports ticket 30). Any <c>{ v, f }</c>-shaped cell (<see cref="HtmlSafeTableCell"/>'s dictionary, or the ad
/// hoc anonymous <c>v</c>/<c>f</c> link cells the Portal/ArcGIS Server Complete Views build directly) is reduced
/// to its plain <c>v</c> - a TSV cell can carry no markup or link, so only the raw, sortable value makes sense
/// there. Several of those raw values (folder/service-name/service-type/Portal-item-id/username/device-id) are
/// parsed straight from untrusted request URIs and can carry any ASCII control character below 0x20, including a
/// literal tab or newline, per <see cref="DetailPagePathSegment"/>'s own doc comment - left alone, either would
/// corrupt the TSV's row/column structure, so every cell's text has TAB/CR/LF replaced with a single space. The
/// same raw values can also carry a leading <c>=</c>, <c>+</c>, <c>-</c>, or <c>@</c> - the formula-trigger
/// characters Excel/LibreOffice/Google Sheets act on when a cell is opened (CSV/TSV formula injection, CWE-1236)
/// - so a string-typed cell's text starting with one of those is prefixed with a single quote, the standard
/// mitigation that keeps the text literal without changing what it reads as.
/// </summary>
public static class TsvWriter
{
    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Renders <paramref name="rows"/> as TSV and writes it under <paramref name="outputDirectory"/>, creating
    /// any intermediate directories as needed - UTF-8 (no BOM), LF-terminated, matching how
    /// <see cref="StaticPageWriter"/> already writes every HTML page.
    /// </summary>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
    /// <param name="outputRelativePath">The TSV file's own output-relative path (the HTML page's path, with its extension swapped to <c>.tsv</c>).</param>
    /// <param name="rows">The table's data, as row-major arrays: the first row is column headers.</param>
    /// <param name="trailingColumnsToDrop">How many of each row's trailing columns to omit - the link-only columns (e.g. "Detail Page") a TSV cannot carry.</param>
#pragma warning disable CC0042 // Four independent inputs (output location, this file's own path, the data, and how much of it to drop); bundling them would just repackage them without benefit.
    public static void Write(
        string outputDirectory, string outputRelativePath, IReadOnlyList<IReadOnlyList<object?>> rows, int trailingColumnsToDrop = 0)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(outputDirectory);
        ArgumentNullException.ThrowIfNull(outputRelativePath);
        ArgumentNullException.ThrowIfNull(rows);

        var fullPath = Path.Combine(outputDirectory, outputRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, Render(rows, trailingColumnsToDrop), _utf8NoBom);
    }

    /// <summary>
    /// Writes the TSV sibling of <paramref name="htmlOutputRelativePath"/> and returns the "Download as TSV"
    /// link's embeddable HTML to prepend to the page body - or an empty string when <paramref name="table"/>
    /// carries no data rows past its header, matching <see cref="GoogleChartsRenderer.RenderTable"/>'s own "No
    /// data for this range." card, which likewise renders nothing for that case. The one place every Complete
    /// View builder turns its already-built table into both outputs, so the "no data means no TSV either" rule
    /// lives once rather than once per builder.
    /// </summary>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
    /// <param name="htmlOutputRelativePath">The Complete View page's own output-relative path.</param>
    /// <param name="table">The table's data, as row-major arrays: the first row is column headers.</param>
    /// <param name="trailingColumnsToDrop">How many of each row's trailing columns to omit - the link-only columns (e.g. "Detail Page") a TSV cannot carry.</param>
    /// <returns>The download link's embeddable body HTML, or an empty string if there was no data to write.</returns>
#pragma warning disable CC0042 // Four independent inputs (output location, this page's own path, the data, and how much of it to drop); bundling them would just repackage them without benefit.
    public static string WriteSiblingAndRenderDownloadLink(
        string outputDirectory, string htmlOutputRelativePath, IReadOnlyList<IReadOnlyList<object?>> table, int trailingColumnsToDrop = 0)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(outputDirectory);
        ArgumentNullException.ThrowIfNull(htmlOutputRelativePath);
        ArgumentNullException.ThrowIfNull(table);

        if (table.Count <= 1)
        {
            return string.Empty;
        }

        var tsvRelativePath = Path.ChangeExtension(htmlOutputRelativePath, ".tsv");
        Write(outputDirectory, tsvRelativePath, table, trailingColumnsToDrop);

        var fileName = Path.GetFileName(tsvRelativePath);
        return $"""<p class="tsv-download-link"><a href="{WebUtility.HtmlEncode(fileName)}">Download as TSV</a></p>""";
    }

    private static string Render(IReadOnlyList<IReadOnlyList<object?>> rows, int trailingColumnsToDrop)
    {
        var builder = new StringBuilder();

        foreach (var row in rows)
        {
            var columnCount = row.Count - trailingColumnsToDrop;

            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                if (columnIndex > 0)
                {
                    builder.Append('\t');
                }

                builder.Append(FormatCell(row[columnIndex]));
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string FormatCell(object? cell)
    {
        var value = UnwrapValue(cell);
        var text = (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
            .Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

        return value is string && IsFormulaTrigger(text) ? "'" + text : text;
    }

    // Only a string-typed value can carry attacker-influenced text; a numeric cell's leading '-' is an ordinary
    // negative number, never a formula to neutralize.
    private static bool IsFormulaTrigger(string text) => text.Length > 0 && text[0] is '=' or '+' or '-' or '@';

    private static object? UnwrapValue(object? cell)
    {
        if (cell is IReadOnlyDictionary<string, string> cellDictionary)
        {
            return cellDictionary.TryGetValue("v", out var dictionaryValue) ? dictionaryValue : cell;
        }

        var valueProperty = cell?.GetType().GetProperty("v");
        return valueProperty is not null ? valueProperty.GetValue(cell) : cell;
    }
}
