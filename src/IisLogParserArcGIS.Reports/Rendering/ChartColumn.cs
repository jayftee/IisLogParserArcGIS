namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// One column of a <c>google.visualization.DataTable</c>, declared explicitly via <c>addColumn</c> rather than
/// inferred from the first data row - inference fails when that row's value for a numeric column happens to be
/// <see langword="null"/> (a real possibility whenever a series has no data on the chart's first plotted date).
/// </summary>
/// <param name="DataType">A Google Charts column type, e.g. <c>"string"</c> or <c>"number"</c>.</param>
/// <param name="Label">The column's display label.</param>
public sealed record ChartColumn(string DataType, string Label);
