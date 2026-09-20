namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// One bar of a <c>google.visualization.BarChart</c> Top-50 Leaderboard, for
/// <see cref="GoogleChartsRenderer.RenderBarChart(string, IReadOnlyList{BarChartEntry}, string, string, ChartTone)"/>.
/// </summary>
/// <param name="Label">The bar's category label (e.g. a folder/service/type identity).</param>
/// <param name="Value">The bar's ranked measure.</param>
public sealed record BarChartEntry(string Label, double Value);
