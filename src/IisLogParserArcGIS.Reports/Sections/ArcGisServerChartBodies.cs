using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Rendering;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the embedded-chart body HTML for one site's four evolution metrics (successful/failed request counts,
/// successful/failed average processing time) - each rendered as a single-series
/// <see cref="GoogleChartsRenderer.RenderAnnotatedTimeLine(string, AnnotatedTimeLineData, string, ChartTone)"/>,
/// per ticket 01's chart-type decision. A failed-outcome chart draws in <see cref="ChartTone.Failure"/> (red),
/// matching a prior version of this tool (ticket 20).
/// </summary>
internal static class ArcGisServerChartBodies
{
    /// <summary>
    /// Builds the successful-requests (or failed-requests) evolution chart body for one site and range. Per-day
    /// values come straight from <see cref="ByArcGisServiceDailyHitTotalsQuery"/>'s own rows - never cumulative.
    /// </summary>
    /// <param name="dailyRows">The site's daily hit totals for the page's range (All or Last 7 Days).</param>
    /// <param name="outcome">Whether this chart shows successful or failed hits.</param>
    /// <param name="elementId">
    /// The HTML id of the chart's container <c>&lt;div&gt;</c> - the caller's own to pick, since a Detail Page
    /// (ticket 13) embeds both ranges' charts on one page and needs the two apart.
    /// </param>
    /// <param name="title">The chart's own title.</param>
    /// <returns>The chart's embeddable body HTML.</returns>
#pragma warning disable CC0042 // Four independent inputs: the data, which half of the outcome split, and two independent rendering strings (id, title).
    public static string BuildHitEvolutionBody(IReadOnlyList<ByArcGisServiceDailyHitTotalRow> dailyRows, RequestOutcome outcome, string elementId, string title)
#pragma warning restore CC0042
    {
        var dates = dailyRows.Select(row => row.LocalDate).ToArray();
        var values = dailyRows.Select(row => (double?)(outcome == RequestOutcome.Successful ? row.SuccessfulHits : row.FailedHits)).ToArray();
        var seriesLabel = outcome == RequestOutcome.Successful ? "Successful Hits" : "Failed Hits";
        var tone = outcome == RequestOutcome.Successful ? ChartTone.Default : ChartTone.Failure;

        return GoogleChartsRenderer.RenderAnnotatedTimeLine(elementId, new AnnotatedTimeLineData(dates, values, seriesLabel), title, tone);
    }

    /// <summary>
    /// Builds the average-processing-time-for-successful-requests (or failed-requests) evolution chart body for
    /// one site and range. Recomputes <c>SUM(time)/SUM(hits)</c> fresh per day from
    /// <see cref="ByArcGisServiceDailyAverageTimeQuery"/>'s own rows, per ticket 11's acceptance criteria - never
    /// reusing <see cref="ByArcGisServiceDailyHitTotalRow"/>'s rows. A day with zero hits of the relevant outcome
    /// (e.g. a site had failed hits but no successful ones) has no defined average and renders as a gap, not an
    /// invented zero.
    /// </summary>
    /// <param name="dailyRows">The site's daily time-taken totals for the page's range (All or Last 7 Days).</param>
    /// <param name="outcome">Whether this chart shows the successful or failed average.</param>
    /// <param name="elementId">
    /// The HTML id of the chart's container <c>&lt;div&gt;</c> - the caller's own to pick, since a Detail Page
    /// (ticket 13) embeds both ranges' charts on one page and needs the two apart.
    /// </param>
    /// <param name="title">The chart's own title.</param>
    /// <returns>The chart's embeddable body HTML.</returns>
#pragma warning disable CC0042 // Four independent inputs: the data, which half of the outcome split, and two independent rendering strings (id, title).
    public static string BuildAverageTimeEvolutionBody(IReadOnlyList<ByArcGisServiceDailyAverageTimeRow> dailyRows, RequestOutcome outcome, string elementId, string title)
#pragma warning restore CC0042
    {
        var dates = dailyRows.Select(row => row.LocalDate).ToArray();
        var values = dailyRows.Select(row => ComputeAverage(row, outcome)).ToArray();
        var seriesLabel = outcome == RequestOutcome.Successful ? "Average Time Taken - Successful (s)" : "Average Time Taken - Failed (s)";
        var tone = outcome == RequestOutcome.Successful ? ChartTone.Default : ChartTone.Failure;

        return GoogleChartsRenderer.RenderAnnotatedTimeLine(elementId, new AnnotatedTimeLineData(dates, values, seriesLabel), title, tone);
    }

    private static double? ComputeAverage(ByArcGisServiceDailyAverageTimeRow row, RequestOutcome outcome)
    {
        var hits = outcome == RequestOutcome.Successful ? row.SuccessfulHits : row.FailedHits;
        var totalTimeTakenSecond = outcome == RequestOutcome.Successful ? row.SuccessfulTimeTakenSecond : row.FailedTimeTakenSecond;

        return hits > 0 ? Math.Round(totalTimeTakenSecond / hits, 3) : null;
    }
}
