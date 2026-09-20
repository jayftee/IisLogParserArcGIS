namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Whether a <see cref="GoogleChartsRenderer.RenderBarChart(string, IReadOnlyList{BarChartEntry}, string, string, ChartTone)"/>
/// or <see cref="GoogleChartsRenderer.RenderAnnotatedTimeLine(string, AnnotatedTimeLineData, string, ChartTone)"/>
/// call draws in Google Charts' own default series color or in red - so a "Failed"-scoped metric (a Leaderboard's
/// bars, an evolution chart's line) reads as visually distinct from a "Successful" one at a glance, the way a
/// prior version of this tool did.
/// </summary>
public enum ChartTone
{
    /// <summary>
    /// Google Charts' own default series color. Every chart not specifically scoped to a failed/erroring outcome
    /// uses this.
    /// </summary>
    Default,

    /// <summary>
    /// Red - "Sunset Dark" (Pantone 704 CP, <c>#9e2b33</c>), from the same brand palette (<c>docs/ColorPalette.png</c>)
    /// every other Dashboard color is drawn from. Used for every chart scoped to a failed/erroring outcome.
    /// </summary>
    Failure,
}
