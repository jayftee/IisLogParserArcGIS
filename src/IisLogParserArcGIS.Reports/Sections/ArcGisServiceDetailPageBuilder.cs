using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds one Detail Page per ArcGIS Server service (ticket 13): that service's own successful/failed request-
/// count and average-processing-time evolution, for every service the ArcGIS Server Complete View (ticket 12)
/// lists - the same <see cref="ByArcGisServiceRangeTotalsQuery"/> result, so exactly the rows the Complete View
/// links to get a page here, no more and no fewer. A Detail Page has no sidebar entry and is reachable only by
/// following a link from that Complete View, so - unlike ticket 11's per-site metric pages - its All and Last 7
/// Days variants share one page with an in-page toggle rather than living at two separate URLs.
/// </summary>
public static class ArcGisServiceDetailPageBuilder
{
    private const string SuccessfulRequestsChartId = "arcgis-service-detail-successful-requests-chart";
    private const string FailedRequestsChartId = "arcgis-service-detail-failed-requests-chart";
    private const string AverageTimeSuccessfulChartId = "arcgis-service-detail-average-time-successful-chart";
    private const string AverageTimeFailedChartId = "arcgis-service-detail-average-time-failed-chart";

    /// <summary>
    /// Builds every ArcGIS Server service Detail Page.
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

        var services = new ByArcGisServiceRangeTotalsQuery(connection)
            .GetRangeTotals(boundaries.YearStart, boundaries.Today, settings.IncludedRoots)
            .ToArray();

        var hitTotalsQuery = new ByArcGisServiceDetailDailyHitTotalsQuery(connection);
        var averageTimeQuery = new ByArcGisServiceDetailDailyAverageTimeQuery(connection);

        foreach (var service in services)
        {
            var identity = new ArcGisServiceIdentity
            {
                Site = service.Site,
                Folder = service.Folder,
                ServiceName = service.ServiceName,
                ServiceType = service.ServiceType,
            };

            StaticPageWriter.Write(outputDirectory, BuildPageRequest(identity, hitTotalsQuery, averageTimeQuery, boundaries, generatedAtUtc, settings.IncludedRoots));
        }
    }

#pragma warning disable CC0042 // Six independent inputs bundled by no fewer than every other page-request-building helper in this project already threads.
    private static PageShellRequest BuildPageRequest(
        ArcGisServiceIdentity identity,
        ByArcGisServiceDetailDailyHitTotalsQuery hitTotalsQuery,
        ByArcGisServiceDetailDailyAverageTimeQuery averageTimeQuery,
        RegenerationRunBoundaries boundaries,
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<string> includedRoots)
#pragma warning restore CC0042
    {
        var allHitRows = hitTotalsQuery.GetDailyTotals(boundaries.YearStart, boundaries.Today, identity).ToArray();
        var last7HitRows = allHitRows.Where(row => row.LocalDate >= boundaries.Last7DaysStart).ToArray();
        var allAverageTimeRows = averageTimeQuery.GetDailyAverageTimeTotals(boundaries.YearStart, boundaries.Today, identity).ToArray();
        var last7AverageTimeRows = allAverageTimeRows.Where(row => row.LocalDate >= boundaries.Last7DaysStart).ToArray();

        var titlePrefix = $"ArcGIS Server » {identity.Site} » {ArcGisServiceLabelFormatter.Format(identity.Folder, identity.ServiceName, identity.ServiceType)}";
        var href = DashboardSidebar.ArcGisServiceDetailHref(identity.Site, identity.Folder, identity.ServiceName, identity.ServiceType);
        var backHref = RelativeLinkHelper.ComputeRootPrefix(href) + DashboardSidebar.ArcGisServerCompleteViewHref;

        return new PageShellRequest
        {
            Title = titlePrefix,
            OutputRelativePath = href,
            ActiveHref = href,
            BodyHtml = BuildPageBody(titlePrefix, backHref, allHitRows, last7HitRows, allAverageTimeRows, last7AverageTimeRows),
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = includedRoots,
        };
    }

#pragma warning disable CC0042 // Six independent per-range chart inputs a single Detail Page body needs; splitting further would just repackage them without benefit.
    private static string BuildPageBody(
        string titlePrefix,
        string backHref,
        IReadOnlyList<ByArcGisServiceDailyHitTotalRow> allHitRows,
        IReadOnlyList<ByArcGisServiceDailyHitTotalRow> last7HitRows,
        IReadOnlyList<ByArcGisServiceDailyAverageTimeRow> allAverageTimeRows,
        IReadOnlyList<ByArcGisServiceDailyAverageTimeRow> last7AverageTimeRows)
#pragma warning restore CC0042
    {
        var allChartsHtml = BuildChartsHtml(titlePrefix, DashboardDateRange.All, allHitRows, allAverageTimeRows);
        var last7ChartsHtml = BuildChartsHtml(titlePrefix, DashboardDateRange.Last7Days, last7HitRows, last7AverageTimeRows);

        return DetailRangeToggle.Render(backHref, allChartsHtml, last7ChartsHtml);
    }

#pragma warning disable CC0042 // Four independent inputs (title, range, both rows sets) a single range's four charts need.
    private static string BuildChartsHtml(
        string titlePrefix,
        DashboardDateRange range,
        IReadOnlyList<ByArcGisServiceDailyHitTotalRow> hitRows,
        IReadOnlyList<ByArcGisServiceDailyAverageTimeRow> averageTimeRows)
#pragma warning restore CC0042
    {
        var rangeSlug = RangeSlug(range);
        var rangeLabel = range == DashboardDateRange.Last7Days ? "Last 7 Days" : "All";
        var charts = new[]
        {
            ArcGisServerChartBodies.BuildHitEvolutionBody(
                hitRows, RequestOutcome.Successful, $"{SuccessfulRequestsChartId}-{rangeSlug}", $"{titlePrefix} » Successful Requests » {rangeLabel}"),
            ArcGisServerChartBodies.BuildHitEvolutionBody(
                hitRows, RequestOutcome.Failed, $"{FailedRequestsChartId}-{rangeSlug}", $"{titlePrefix} » Failed Requests » {rangeLabel}"),
            ArcGisServerChartBodies.BuildAverageTimeEvolutionBody(
                averageTimeRows, RequestOutcome.Successful, $"{AverageTimeSuccessfulChartId}-{rangeSlug}", $"{titlePrefix} » Average Time (Successful) » {rangeLabel}"),
            ArcGisServerChartBodies.BuildAverageTimeEvolutionBody(
                averageTimeRows, RequestOutcome.Failed, $"{AverageTimeFailedChartId}-{rangeSlug}", $"{titlePrefix} » Average Time (Failed) » {rangeLabel}"),
        };

        return string.Join("\n", charts);
    }

    private static string RangeSlug(DashboardDateRange range) => range == DashboardDateRange.Last7Days ? "last-7-days" : "all";
}
