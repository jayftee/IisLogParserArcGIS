using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the Portal section (ticket 14): Portal's own successful/failed request-count evolution and its
/// Top-50 Successful/Failed-Hits Leaderboards, each with All and Last 7 Days variants - 8 pages total. Portal
/// renders unconditionally, regardless of the Included Root allow-list, since the by-Portal-item aggregate
/// table's rows are already scoped to the configured Portal Web Adaptor at ingest time (per ticket 04's
/// resolution) - no Root-filtering logic is added here. Every figure is computed on the fly against the
/// aggregate database each run; no rollup table is introduced (per ticket 03).
/// </summary>
public static class PortalSectionBuilder
{
    private const string SuccessfulRequestsChartId = "portal-successful-requests-chart";
    private const string FailedRequestsChartId = "portal-failed-requests-chart";
    private const string LeaderboardSuccessfulHitsChartId = "portal-leaderboard-successful-hits-chart";
    private const string LeaderboardFailedHitsChartId = "portal-leaderboard-failed-hits-chart";

    /// <summary>
    /// Builds every page in the Portal section.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="boundaries">This Regeneration Run's resolved year-to-date boundaries.</param>
    /// <param name="generatedAtUtc">The instant this Regeneration Run started, for every page's footer.</param>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
#pragma warning disable CC0042 // Five independent inputs threaded from RegenerationRun.Run's own "primary seam", matching every other section builder's own justified suppression.
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

        var context = new PortalBuildContext(
            connection,
            settings.IncludedRoots,
            boundaries.YearStart,
            boundaries.Last7DaysStart,
            boundaries.Today,
            generatedAtUtc);

        foreach (var request in BuildPageRequests(context))
        {
            StaticPageWriter.Write(outputDirectory, request);
        }
    }

    private static IEnumerable<PageShellRequest> BuildPageRequests(PortalBuildContext context)
    {
        return BuildEvolutionPageRequests(context).Concat(BuildLeaderboardPageRequests(context));
    }

    private static IEnumerable<PageShellRequest> BuildEvolutionPageRequests(PortalBuildContext context)
    {
        var dailyHitTotalsQuery = new ByPortalItemDailyHitTotalsQuery(context.Connection);
        var allRows = dailyHitTotalsQuery.GetDailyTotals(context.YearStart, context.Today).ToArray();
        var last7Rows = allRows.Where(row => row.LocalDate >= context.Last7DaysStart).ToArray();

        var metrics = new[]
        {
            new MetricPageContent(
                DashboardSidebar.PortalSuccessfulRequestsSlug,
                "Portal » Successful Requests",
                PortalChartBodies.BuildHitEvolutionBody(allRows, RequestOutcome.Successful, SuccessfulRequestsChartId, "Portal » Successful Requests » All"),
                PortalChartBodies.BuildHitEvolutionBody(last7Rows, RequestOutcome.Successful, SuccessfulRequestsChartId, "Portal » Successful Requests » Last 7 Days")),
            new MetricPageContent(
                DashboardSidebar.PortalFailedRequestsSlug,
                "Portal » Failed Requests",
                PortalChartBodies.BuildHitEvolutionBody(allRows, RequestOutcome.Failed, FailedRequestsChartId, "Portal » Failed Requests » All"),
                PortalChartBodies.BuildHitEvolutionBody(last7Rows, RequestOutcome.Failed, FailedRequestsChartId, "Portal » Failed Requests » Last 7 Days")),
        };

        return metrics.SelectMany(metric => BuildMetricPageRequests(context, metric));
    }

    private static IEnumerable<PageShellRequest> BuildLeaderboardPageRequests(PortalBuildContext context)
    {
        var topSuccessfulHitsQuery = new ByPortalItemTopSuccessfulHitsQuery(context.Connection);
        var topFailedHitsQuery = new ByPortalItemTopFailedHitsQuery(context.Connection);

        var successfulHitsAll = topSuccessfulHitsQuery.GetTop50(context.YearStart, context.Today).ToArray();
        var successfulHitsLast7 = topSuccessfulHitsQuery.GetTop50(context.Last7DaysStart, context.Today).ToArray();
        var failedHitsAll = topFailedHitsQuery.GetTop50(context.YearStart, context.Today).ToArray();
        var failedHitsLast7 = topFailedHitsQuery.GetTop50(context.Last7DaysStart, context.Today).ToArray();

        var metrics = new[]
        {
            new MetricPageContent(
                DashboardSidebar.PortalLeaderboardSuccessfulHitsSlug,
                "Portal » Leaderboard » Successful Hits",
                PortalLeaderboardBodies.BuildHitsLeaderboardBody(
                    successfulHitsAll, RequestOutcome.Successful, LeaderboardSuccessfulHitsChartId, "Successful Hits", "Portal » Leaderboard » Successful Hits » All"),
                PortalLeaderboardBodies.BuildHitsLeaderboardBody(
                    successfulHitsLast7, RequestOutcome.Successful, LeaderboardSuccessfulHitsChartId, "Successful Hits", "Portal » Leaderboard » Successful Hits » Last 7 Days")),
            new MetricPageContent(
                DashboardSidebar.PortalLeaderboardFailedHitsSlug,
                "Portal » Leaderboard » Failed Hits",
                PortalLeaderboardBodies.BuildHitsLeaderboardBody(
                    failedHitsAll, RequestOutcome.Failed, LeaderboardFailedHitsChartId, "Failed Hits", "Portal » Leaderboard » Failed Hits » All"),
                PortalLeaderboardBodies.BuildHitsLeaderboardBody(
                    failedHitsLast7, RequestOutcome.Failed, LeaderboardFailedHitsChartId, "Failed Hits", "Portal » Leaderboard » Failed Hits » Last 7 Days")),
        };

        return metrics.SelectMany(metric => BuildMetricPageRequests(context, metric));
    }

    private static IEnumerable<PageShellRequest> BuildMetricPageRequests(PortalBuildContext context, MetricPageContent content)
    {
        var allHref = DashboardSidebar.PortalPageHref(content.MetricSlug, DashboardDateRange.All);
        var last7Href = DashboardSidebar.PortalPageHref(content.MetricSlug, DashboardDateRange.Last7Days);

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

#pragma warning disable CC0042 // Four independent per-metric values bundled to keep BuildMetricPageRequests' own parameter count low; matches ArcGisServerSectionBuilder's own justified suppression.
    private sealed record MetricPageContent(string MetricSlug, string TitlePrefix, string AllBody, string Last7Body);
#pragma warning restore CC0042
}
