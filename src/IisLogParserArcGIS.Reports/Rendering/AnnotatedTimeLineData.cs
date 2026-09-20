namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// A single-series <c>google.visualization.AnnotatedTimeLine</c>'s dates and values, for
/// <see cref="GoogleChartsRenderer.RenderAnnotatedTimeLine(string, AnnotatedTimeLineData, string, ChartTone)"/>. A
/// <see langword="null"/> value renders as a gap on that date - e.g. a site had failed hits but no successful
/// hits that day, so no successful-request average time is defined for it - never an invented zero.
/// </summary>
/// <param name="Dates">The series' real dates, in order - only dates the underlying entity actually has data for.</param>
/// <param name="Values">The series' values, one per <paramref name="Dates"/> entry, in the same order.</param>
/// <param name="SeriesLabel">The series' display label.</param>
public sealed record AnnotatedTimeLineData(IReadOnlyList<DateOnly> Dates, IReadOnlyList<double?> Values, string SeriesLabel);
