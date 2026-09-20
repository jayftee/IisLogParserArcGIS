using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds one Detail Page per Portal item (ticket 16): that item's own successful/failed request-count evolution,
/// for every item the Portal Complete View (ticket 15) lists - the same <see cref="ByPortalItemRangeTotalsQuery"/>
/// result, so exactly the rows the Complete View links to get a page here, no more and no fewer. A Detail Page has
/// no sidebar entry and is reachable only by following a link from that Complete View, so - mirroring the ArcGIS
/// Server service Detail Page (ticket 13) - its All and Last 7 Days variants share one page with an in-page toggle
/// rather than living at two separate URLs. Unlike a service's Detail Page, Portal's aggregate table carries only
/// one combined time-taken figure (no successful/failed split), so this page has no average-time charts.
/// </summary>
public static class PortalItemDetailPageBuilder
{
    private const string SuccessfulRequestsChartId = "portal-item-detail-successful-requests-chart";
    private const string FailedRequestsChartId = "portal-item-detail-failed-requests-chart";

    /// <summary>
    /// Builds every Portal item Detail Page.
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

        var items = new ByPortalItemRangeTotalsQuery(connection)
            .GetRangeTotals(boundaries.YearStart, boundaries.Today)
            .ToArray();

        var hitTotalsQuery = new ByPortalItemDetailDailyHitTotalsQuery(connection);

        foreach (var item in items)
        {
            StaticPageWriter.Write(outputDirectory, BuildPageRequest(item.PortalItemId, hitTotalsQuery, boundaries, generatedAtUtc, settings));
        }
    }

#pragma warning disable CC0042 // Five independent inputs bundled by no fewer than every other page-request-building helper in this project already threads.
    private static PageShellRequest BuildPageRequest(
        string portalItemId,
        ByPortalItemDetailDailyHitTotalsQuery hitTotalsQuery,
        RegenerationRunBoundaries boundaries,
        DateTimeOffset generatedAtUtc,
        ReportsSettings settings)
#pragma warning restore CC0042
    {
        var allHitRows = hitTotalsQuery.GetDailyTotals(boundaries.YearStart, boundaries.Today, portalItemId).ToArray();
        var last7HitRows = allHitRows.Where(row => row.LocalDate >= boundaries.Last7DaysStart).ToArray();

        var title = $"Portal » {portalItemId}";
        var href = DashboardSidebar.PortalItemDetailHref(portalItemId);
        var backHref = RelativeLinkHelper.ComputeRootPrefix(href) + DashboardSidebar.PortalCompleteViewHref;

        return new PageShellRequest
        {
            Title = title,
            OutputRelativePath = href,
            ActiveHref = href,
            BodyHtml = BuildPageBody(title, backHref, allHitRows, last7HitRows),
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = settings.IncludedRoots,
        };
    }

#pragma warning disable CC0042 // Four independent inputs a single Detail Page body needs: the title/back-link inputs and both ranges' rows.
    private static string BuildPageBody(
        string title,
        string backHref,
        IReadOnlyList<ByPortalItemDailyHitTotalRow> allHitRows,
        IReadOnlyList<ByPortalItemDailyHitTotalRow> last7HitRows)
#pragma warning restore CC0042
    {
        var allChartsHtml = BuildChartsHtml(title, DashboardDateRange.All, allHitRows);
        var last7ChartsHtml = BuildChartsHtml(title, DashboardDateRange.Last7Days, last7HitRows);

        return DetailRangeToggle.Render(backHref, allChartsHtml, last7ChartsHtml);
    }

    private static string BuildChartsHtml(string title, DashboardDateRange range, IReadOnlyList<ByPortalItemDailyHitTotalRow> hitRows)
    {
        var rangeSlug = RangeSlug(range);
        var rangeLabel = range == DashboardDateRange.Last7Days ? "Last 7 Days" : "All";
        var charts = new[]
        {
            PortalChartBodies.BuildHitEvolutionBody(
                hitRows, RequestOutcome.Successful, $"{SuccessfulRequestsChartId}-{rangeSlug}", $"{title} » Successful Requests » {rangeLabel}"),
            PortalChartBodies.BuildHitEvolutionBody(
                hitRows, RequestOutcome.Failed, $"{FailedRequestsChartId}-{rangeSlug}", $"{title} » Failed Requests » {rangeLabel}"),
        };

        return string.Join("\n", charts);
    }

    private static string RangeSlug(DashboardDateRange range) => range == DashboardDateRange.Last7Days ? "last-7-days" : "all";
}
