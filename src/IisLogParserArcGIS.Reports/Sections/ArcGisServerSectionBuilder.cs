using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the ArcGIS Server section (ticket 11): one collapsible sidebar group per Included Root, each with its
/// own 8 metrics (successful/failed request-count evolution, successful/failed average-processing-time
/// evolution) and 4 Top-50 Leaderboards (by successful hits, failed hits, average time successful, average time
/// failed), every one of the 12 with All and Last 7 Days variants - 224 pages across 14 Included Roots at the
/// real Root count. Every figure is computed on the fly against the aggregate database each run; no rollup
/// table is introduced (per ticket 03).
/// </summary>
public static class ArcGisServerSectionBuilder
{
    /// <summary>
    /// Builds every page in the ArcGIS Server section: one group of pages per Root in
    /// <paramref name="settings"/>'s Included Root allow-list. A Root present in the aggregate database but
    /// absent from that list never appears here.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="boundaries">This Regeneration Run's resolved year-to-date boundaries.</param>
    /// <param name="generatedAtUtc">The instant this Regeneration Run started, for every page's footer.</param>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
#pragma warning disable CC0042 // Five independent inputs threaded from RegenerationRun.Run's own "primary seam", matching SummarySectionBuilder.Build's own justified suppression.
    public static void Build(
        SqliteConnection connection,
        ReportsSettings settings,
        RegenerationRunBoundaries boundaries,
        DateTimeOffset generatedAtUtc,
        string outputDirectory)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        var context = new ArcGisServerBuildContext(
            new ArcGisServerQueries(connection),
            settings.IncludedRoots,
            boundaries.YearStart,
            boundaries.Last7DaysStart,
            boundaries.Today,
            generatedAtUtc);

        foreach (var site in settings.IncludedRoots)
        {
            foreach (var request in BuildSitePageRequests(context, site))
            {
                StaticPageWriter.Write(outputDirectory, request);
            }
        }
    }

    private static IEnumerable<PageShellRequest> BuildSitePageRequests(ArcGisServerBuildContext context, string site)
    {
        foreach (var request in BuildEvolutionPageRequests(context, site))
        {
            yield return request;
        }

        foreach (var request in BuildLeaderboardPageRequests(context, site))
        {
            yield return request;
        }
    }

    private static IEnumerable<PageShellRequest> BuildEvolutionPageRequests(ArcGisServerBuildContext context, string site)
    {
        var allHitRows = context.Queries.HitTotals.GetDailyTotals(context.YearStart, context.Today, site).ToArray();
        var last7HitRows = allHitRows.Where(row => row.LocalDate >= context.Last7DaysStart).ToArray();
        var allAverageTimeRows = context.Queries.AverageTimeTotals.GetDailyAverageTimeTotals(context.YearStart, context.Today, site).ToArray();
        var last7AverageTimeRows = allAverageTimeRows.Where(row => row.LocalDate >= context.Last7DaysStart).ToArray();
        var titlePrefix = $"ArcGIS Server » {site}";

        const string SuccessfulRequestsChartId = "arcgis-server-successful-requests-chart";
        const string FailedRequestsChartId = "arcgis-server-failed-requests-chart";
        const string AverageTimeSuccessfulChartId = "arcgis-server-average-time-successful-chart";
        const string AverageTimeFailedChartId = "arcgis-server-average-time-failed-chart";

        var metrics = new[]
        {
            new MetricPageContent(
                DashboardSidebar.ArcGisServerSuccessfulRequestsSlug,
                $"{titlePrefix} » Successful Requests",
                ArcGisServerChartBodies.BuildHitEvolutionBody(allHitRows, RequestOutcome.Successful, SuccessfulRequestsChartId, $"{titlePrefix} » Successful Requests » All"),
                ArcGisServerChartBodies.BuildHitEvolutionBody(last7HitRows, RequestOutcome.Successful, SuccessfulRequestsChartId, $"{titlePrefix} » Successful Requests » Last 7 Days")),
            new MetricPageContent(
                DashboardSidebar.ArcGisServerFailedRequestsSlug,
                $"{titlePrefix} » Failed Requests",
                ArcGisServerChartBodies.BuildHitEvolutionBody(allHitRows, RequestOutcome.Failed, FailedRequestsChartId, $"{titlePrefix} » Failed Requests » All"),
                ArcGisServerChartBodies.BuildHitEvolutionBody(last7HitRows, RequestOutcome.Failed, FailedRequestsChartId, $"{titlePrefix} » Failed Requests » Last 7 Days")),
            new MetricPageContent(
                DashboardSidebar.ArcGisServerAverageTimeSuccessfulSlug,
                $"{titlePrefix} » Average Time (Successful)",
                ArcGisServerChartBodies.BuildAverageTimeEvolutionBody(
                    allAverageTimeRows, RequestOutcome.Successful, AverageTimeSuccessfulChartId, $"{titlePrefix} » Average Time (Successful) » All"),
                ArcGisServerChartBodies.BuildAverageTimeEvolutionBody(
                    last7AverageTimeRows, RequestOutcome.Successful, AverageTimeSuccessfulChartId, $"{titlePrefix} » Average Time (Successful) » Last 7 Days")),
            new MetricPageContent(
                DashboardSidebar.ArcGisServerAverageTimeFailedSlug,
                $"{titlePrefix} » Average Time (Failed)",
                ArcGisServerChartBodies.BuildAverageTimeEvolutionBody(
                    allAverageTimeRows, RequestOutcome.Failed, AverageTimeFailedChartId, $"{titlePrefix} » Average Time (Failed) » All"),
                ArcGisServerChartBodies.BuildAverageTimeEvolutionBody(
                    last7AverageTimeRows, RequestOutcome.Failed, AverageTimeFailedChartId, $"{titlePrefix} » Average Time (Failed) » Last 7 Days")),
        };

        return metrics.SelectMany(metric => BuildMetricPageRequests(context, site, metric));
    }

#pragma warning disable CC0034 // Four identical MetricPageContent entries, one line longer now RequestOutcome (ticket 20's red-for-failed) is threaded through; a helper would just indirect them.
    private static IEnumerable<PageShellRequest> BuildLeaderboardPageRequests(ArcGisServerBuildContext context, string site)
#pragma warning restore CC0034
    {
        var titlePrefix = $"ArcGIS Server » {site}";

        var successfulHitsAll = context.Queries.TopSuccessfulHits.GetTop50(context.YearStart, context.Today, site).ToArray();
        var successfulHitsLast7 = context.Queries.TopSuccessfulHits.GetTop50(context.Last7DaysStart, context.Today, site).ToArray();
        var failedHitsAll = context.Queries.TopFailedHits.GetTop50(context.YearStart, context.Today, site).ToArray();
        var failedHitsLast7 = context.Queries.TopFailedHits.GetTop50(context.Last7DaysStart, context.Today, site).ToArray();
        var averageSuccessfulTimeAll = context.Queries.TopAverageSuccessfulTime.GetTop50(context.YearStart, context.Today, site).ToArray();
        var averageSuccessfulTimeLast7 = context.Queries.TopAverageSuccessfulTime.GetTop50(context.Last7DaysStart, context.Today, site).ToArray();
        var averageFailedTimeAll = context.Queries.TopAverageFailedTime.GetTop50(context.YearStart, context.Today, site).ToArray();
        var averageFailedTimeLast7 = context.Queries.TopAverageFailedTime.GetTop50(context.Last7DaysStart, context.Today, site).ToArray();

        var metrics = new[]
        {
            new MetricPageContent(
                DashboardSidebar.ArcGisServerLeaderboardSuccessfulHitsSlug,
                $"{titlePrefix} » Leaderboard » Successful Hits",
                ArcGisServerLeaderboardBodies.BuildHitsLeaderboardBody(
                    successfulHitsAll,
                    RequestOutcome.Successful,
                    "arcgis-server-leaderboard-successful-hits-chart",
                    "Successful Hits",
                    $"{titlePrefix} » Leaderboard » Successful Hits » All"),
                ArcGisServerLeaderboardBodies.BuildHitsLeaderboardBody(
                    successfulHitsLast7,
                    RequestOutcome.Successful,
                    "arcgis-server-leaderboard-successful-hits-chart",
                    "Successful Hits",
                    $"{titlePrefix} » Leaderboard » Successful Hits » Last 7 Days")),
            new MetricPageContent(
                DashboardSidebar.ArcGisServerLeaderboardFailedHitsSlug,
                $"{titlePrefix} » Leaderboard » Failed Hits",
                ArcGisServerLeaderboardBodies.BuildHitsLeaderboardBody(
                    failedHitsAll,
                    RequestOutcome.Failed,
                    "arcgis-server-leaderboard-failed-hits-chart",
                    "Failed Hits",
                    $"{titlePrefix} » Leaderboard » Failed Hits » All"),
                ArcGisServerLeaderboardBodies.BuildHitsLeaderboardBody(
                    failedHitsLast7,
                    RequestOutcome.Failed,
                    "arcgis-server-leaderboard-failed-hits-chart",
                    "Failed Hits",
                    $"{titlePrefix} » Leaderboard » Failed Hits » Last 7 Days")),
            new MetricPageContent(
                DashboardSidebar.ArcGisServerLeaderboardAverageTimeSuccessfulSlug,
                $"{titlePrefix} » Leaderboard » Average Time (Successful)",
                ArcGisServerLeaderboardBodies.BuildAverageTimeLeaderboardBody(
                    averageSuccessfulTimeAll,
                    RequestOutcome.Successful,
                    "arcgis-server-leaderboard-average-time-successful-chart",
                    "Average Time Taken (s)",
                    $"{titlePrefix} » Leaderboard » Average Time (Successful) » All"),
                ArcGisServerLeaderboardBodies.BuildAverageTimeLeaderboardBody(
                    averageSuccessfulTimeLast7,
                    RequestOutcome.Successful,
                    "arcgis-server-leaderboard-average-time-successful-chart",
                    "Average Time Taken (s)",
                    $"{titlePrefix} » Leaderboard » Average Time (Successful) » Last 7 Days")),
            new MetricPageContent(
                DashboardSidebar.ArcGisServerLeaderboardAverageTimeFailedSlug,
                $"{titlePrefix} » Leaderboard » Average Time (Failed)",
                ArcGisServerLeaderboardBodies.BuildAverageTimeLeaderboardBody(
                    averageFailedTimeAll,
                    RequestOutcome.Failed,
                    "arcgis-server-leaderboard-average-time-failed-chart",
                    "Average Time Taken (s)",
                    $"{titlePrefix} » Leaderboard » Average Time (Failed) » All"),
                ArcGisServerLeaderboardBodies.BuildAverageTimeLeaderboardBody(
                    averageFailedTimeLast7,
                    RequestOutcome.Failed,
                    "arcgis-server-leaderboard-average-time-failed-chart",
                    "Average Time Taken (s)",
                    $"{titlePrefix} » Leaderboard » Average Time (Failed) » Last 7 Days")),
        };

        return metrics.SelectMany(metric => BuildMetricPageRequests(context, site, metric));
    }

    private static IEnumerable<PageShellRequest> BuildMetricPageRequests(ArcGisServerBuildContext context, string site, MetricPageContent content)
    {
        var allHref = DashboardSidebar.ArcGisServerPageHref(site, content.MetricSlug, DashboardDateRange.All);
        var last7Href = DashboardSidebar.ArcGisServerPageHref(site, content.MetricSlug, DashboardDateRange.Last7Days);

        yield return new PageShellRequest
        {
            Title = $"{content.TitlePrefix} » All",
            OutputRelativePath = allHref,
            ActiveHref = allHref,
            BodyHtml = content.AllBody,
            GeneratedAtUtc = context.GeneratedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = context.IncludedRoots,
        };

        yield return new PageShellRequest
        {
            Title = $"{content.TitlePrefix} » Last 7 Days",
            OutputRelativePath = last7Href,
            ActiveHref = last7Href,
            BodyHtml = content.Last7Body,
            GeneratedAtUtc = context.GeneratedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = context.IncludedRoots,
        };
    }

#pragma warning disable CC0042 // Four independent per-metric values bundled to keep BuildMetricPageRequests' own parameter count low; splitting further would just repackage them without benefit.
    private sealed record MetricPageContent(string MetricSlug, string TitlePrefix, string AllBody, string Last7Body);
#pragma warning restore CC0042
}
