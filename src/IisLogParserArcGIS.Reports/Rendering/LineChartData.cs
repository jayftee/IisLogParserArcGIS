namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// A <c>google.visualization.LineChart</c>'s explicitly-typed columns and row-major data, for
/// <see cref="GoogleChartsRenderer.RenderLineChart(string, LineChartData, string)"/>.
/// </summary>
/// <param name="Columns">The chart's columns, in order - the category column, then one series per column.</param>
/// <param name="Rows">The chart's data, as row-major arrays matching <paramref name="Columns"/>' order.</param>
public sealed record LineChartData(IReadOnlyList<ChartColumn> Columns, IReadOnlyList<IReadOnlyList<object?>> Rows);
