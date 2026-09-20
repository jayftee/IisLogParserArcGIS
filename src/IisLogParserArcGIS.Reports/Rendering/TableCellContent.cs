namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Whether a <see cref="GoogleChartsRenderer.RenderTable(string, IReadOnlyList{IReadOnlyList{object}}, TablePaging, TableCellContent, string)"/>
/// call turns on the widget's <c>allowHtml</c> option, so a cell can carry real markup (e.g. a link to that
/// row's own Detail Page, per ticket 13) instead of being rendered as literal text.
/// </summary>
public enum TableCellContent
{
    /// <summary>
    /// Every cell renders as literal text - any markup characters are escaped, not interpreted. The default,
    /// and the only mode ticket 12's Complete View originally needed.
    /// </summary>
    PlainText,

    /// <summary>
    /// Cells may carry HTML markup, which renders as real elements rather than escaped text.
    /// </summary>
    Html,
}
