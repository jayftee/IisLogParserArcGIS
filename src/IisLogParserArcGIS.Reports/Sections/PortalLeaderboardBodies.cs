using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Rendering;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the embedded-chart body HTML for Portal's two Top-50 Leaderboards (by successful hits, failed hits) -
/// each rendered as a <see cref="GoogleChartsRenderer.RenderBarChart(string, IReadOnlyList{BarChartEntry}, string, string, ChartTone)"/>,
/// per ticket 01's chart-type decision. Rows are rendered in the order the query already ranked them - no
/// secondary sort. Bars are labeled with the raw <c>portal_item_id</c> - per ticket 05's resolution, no title
/// resolution is attempted for a Portal item. The Failed Hits Leaderboard draws its bars in
/// <see cref="ChartTone.Failure"/> (red), matching a prior version of this tool (ticket 20).
/// </summary>
internal static class PortalLeaderboardBodies
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
    public static string BuildHitsLeaderboardBody(IReadOnlyList<PortalItemHitsLeaderboardRow> rows, RequestOutcome outcome, string elementId, string valueLabel, string title)
#pragma warning restore CC0042
    {
        var entries = rows.Select(row => new BarChartEntry(row.PortalItemId, row.Hits)).ToArray();
        var tone = outcome == RequestOutcome.Successful ? ChartTone.Default : ChartTone.Failure;

        return GoogleChartsRenderer.RenderBarChart(elementId, entries, valueLabel, title, tone);
    }
}
