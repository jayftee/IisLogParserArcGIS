using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Rendering;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the embedded-chart body HTML for Portal's two evolution metrics (successful/failed request counts) -
/// each rendered as a single-series
/// <see cref="GoogleChartsRenderer.RenderAnnotatedTimeLine(string, AnnotatedTimeLineData, string, ChartTone)"/>,
/// per ticket 01's chart-type decision. Mirrors <see cref="ArcGisServerChartBodies.BuildHitEvolutionBody"/>,
/// including drawing a failed-outcome chart in <see cref="ChartTone.Failure"/> (red, ticket 20), but summed
/// across every Portal item rather than scoped to a single site.
/// </summary>
internal static class PortalChartBodies
{
    /// <summary>
    /// Builds the successful-requests (or failed-requests) evolution chart body for one range. Per-day values
    /// come straight from <see cref="ByPortalItemDailyHitTotalsQuery"/>'s own rows - never cumulative.
    /// </summary>
    /// <param name="dailyRows">Portal's daily hit totals for the page's range (All or Last 7 Days).</param>
    /// <param name="outcome">Whether this chart shows successful or failed hits.</param>
    /// <param name="elementId">The HTML id of the chart's container <c>&lt;div&gt;</c>.</param>
    /// <param name="title">The chart's own title.</param>
    /// <returns>The chart's embeddable body HTML.</returns>
#pragma warning disable CC0042 // Four independent inputs: the data, which half of the outcome split, and two independent rendering strings (id, title).
    public static string BuildHitEvolutionBody(IReadOnlyList<ByPortalItemDailyHitTotalRow> dailyRows, RequestOutcome outcome, string elementId, string title)
#pragma warning restore CC0042
    {
        var dates = dailyRows.Select(row => row.LocalDate).ToArray();
        var values = dailyRows.Select(row => (double?)(outcome == RequestOutcome.Successful ? row.SuccessfulHits : row.FailedHits)).ToArray();
        var seriesLabel = outcome == RequestOutcome.Successful ? "Successful Hits" : "Failed Hits";
        var tone = outcome == RequestOutcome.Successful ? ChartTone.Default : ChartTone.Failure;

        return GoogleChartsRenderer.RenderAnnotatedTimeLine(elementId, new AnnotatedTimeLineData(dates, values, seriesLabel), title, tone);
    }
}
