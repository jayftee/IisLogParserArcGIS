namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Composes one Complete View page's body: the "Download as TSV" link (or nothing, when there's no data) ahead
/// of its own <c>google.visualization.Table</c>, paged at <see cref="PageSize"/> rows/page (reports ticket 30).
/// The one place every Complete View builder (Summary, Portal, ArcGIS Server, and each per-device section) turns
/// its already-built table into a page body, so the TSV-plus-table pairing can't drift between them.
/// </summary>
internal static class CompleteViewBody
{
    private const int PageSize = 1000;

    /// <summary>
    /// Builds the body.
    /// </summary>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
    /// <param name="htmlOutputRelativePath">The Complete View page's own output-relative path.</param>
    /// <param name="elementId">The HTML id of the table's <c>&lt;div&gt;</c>.</param>
    /// <param name="table">The table's data, as row-major arrays: the first row is column headers.</param>
    /// <param name="trailingColumnsToDrop">How many of each row's trailing columns the TSV sibling omits - the link-only columns (e.g. "Detail Page") it cannot carry.</param>
    /// <param name="cellContent">Whether a cell may carry real HTML markup (a link to that row's own Detail Page).</param>
    /// <param name="title">An optional title card, shown above the table - see <see cref="GoogleChartsRenderer.RenderTable"/>.</param>
    /// <returns>The page's embeddable body HTML.</returns>
#pragma warning disable CC0042 // Six independent inputs threaded through from each Complete View builder; bundling them would just repackage them without benefit.
    public static string Build(
        string outputDirectory,
        string htmlOutputRelativePath,
        string elementId,
        IReadOnlyList<IReadOnlyList<object?>> table,
        int trailingColumnsToDrop = 0,
        TableCellContent cellContent = TableCellContent.PlainText,
        string? title = null)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(outputDirectory);
        ArgumentNullException.ThrowIfNull(htmlOutputRelativePath);
        ArgumentNullException.ThrowIfNull(elementId);
        ArgumentNullException.ThrowIfNull(table);

        return TsvWriter.WriteSiblingAndRenderDownloadLink(outputDirectory, htmlOutputRelativePath, table, trailingColumnsToDrop)
            + GoogleChartsRenderer.RenderTable(elementId, table, TablePaging.Enabled, cellContent, title, PageSize);
    }
}
