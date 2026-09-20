using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Rendering;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the embedded-chart body HTML for one site's four Top-50 Leaderboards (by successful hits, failed hits,
/// average successful time, average failed time) - each rendered as a
/// <see cref="GoogleChartsRenderer.RenderBarChart(string, IReadOnlyList{BarChartEntry}, string, string, ChartTone)"/>,
/// per ticket 01's chart-type decision. Rows are rendered in the order the query already ranked them - no
/// secondary sort. A failed-outcome Leaderboard draws its bars in <see cref="ChartTone.Failure"/> (red), matching
/// a prior version of this tool (ticket 20).
/// </summary>
internal static class ArcGisServerLeaderboardBodies
{
    /// <summary>
    /// Builds a by-hits Leaderboard body (successful or failed, per <paramref name="outcome"/> - the caller's own
    /// query already scoped <paramref name="rows"/> to match).
    /// </summary>
    /// <param name="rows">The Leaderboard's rows, already ranked descending by the caller's query.</param>
    /// <param name="outcome">Whether this Leaderboard ranks successful or failed hits.</param>
    /// <param name="elementId">The HTML id of the chart's container <c>&lt;div&gt;</c>.</param>
    /// <param name="valueLabel">The ranked measure's display label.</param>
    /// <param name="title">The chart's own title, shown in its own card directly above the chart's card.</param>
    /// <returns>The chart's embeddable body HTML.</returns>
#pragma warning disable CC0042 // Five independent per-leaderboard values (rows, outcome, id, value label, title); bundling would just repackage them without benefit.
    public static string BuildHitsLeaderboardBody(IReadOnlyList<ArcGisServiceHitsLeaderboardRow> rows, RequestOutcome outcome, string elementId, string valueLabel, string title)
#pragma warning restore CC0042
    {
        var entries = rows.Select(row => new BarChartEntry(ArcGisServiceLabelFormatter.Format(row.Folder, row.ServiceName, row.ServiceType), row.Hits)).ToArray();
        var tone = outcome == RequestOutcome.Successful ? ChartTone.Default : ChartTone.Failure;

        return GoogleChartsRenderer.RenderBarChart(elementId, entries, valueLabel, title, tone);
    }

    /// <summary>
    /// Builds a by-average-time Leaderboard body (successful or failed, per <paramref name="outcome"/> - the
    /// caller's own query already scoped <paramref name="rows"/> to match). The query already excludes rows with
    /// zero matching hits, so the division here is always well-defined.
    /// </summary>
    /// <param name="rows">The Leaderboard's rows, already ranked descending by the caller's query.</param>
    /// <param name="outcome">Whether this Leaderboard ranks successful or failed average time.</param>
    /// <param name="elementId">The HTML id of the chart's container <c>&lt;div&gt;</c>.</param>
    /// <param name="valueLabel">The ranked measure's display label.</param>
    /// <param name="title">The chart's own title, shown in its own card directly above the chart's card.</param>
    /// <returns>The chart's embeddable body HTML.</returns>
#pragma warning disable CC0042 // Five independent per-leaderboard values (rows, outcome, id, value label, title); bundling would just repackage them without benefit.
    public static string BuildAverageTimeLeaderboardBody(IReadOnlyList<ArcGisServiceAverageTimeLeaderboardRow> rows, RequestOutcome outcome, string elementId, string valueLabel, string title)
#pragma warning restore CC0042
    {
        var entries = rows
            .Select(row => new BarChartEntry(ArcGisServiceLabelFormatter.Format(row.Folder, row.ServiceName, row.ServiceType), Math.Round(row.TotalTimeTakenSecond / row.Hits, 3)))
            .ToArray();
        var tone = outcome == RequestOutcome.Successful ? ChartTone.Default : ChartTone.Failure;

        return GoogleChartsRenderer.RenderBarChart(elementId, entries, valueLabel, title, tone);
    }
}
