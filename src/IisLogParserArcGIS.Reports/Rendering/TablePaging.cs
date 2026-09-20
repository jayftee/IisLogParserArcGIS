namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Whether a <see cref="GoogleChartsRenderer.RenderTable(string, IReadOnlyList{IReadOnlyList{object}}, TablePaging, TableCellContent, string)"/>
/// call turns on the widget's built-in <c>page: 'enable'</c> paging.
/// </summary>
public enum TablePaging
{
    /// <summary>
    /// No paging - every row renders into the DOM at once. Fine for a small, fixed-size table (e.g. one row
    /// per Root).
    /// </summary>
    Disabled,

    /// <summary>
    /// Paging enabled, so DOM cost stays bounded regardless of row count - required for an uncapped Complete
    /// View (per ticket 01/12).
    /// </summary>
    Enabled,
}
