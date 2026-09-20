using System.Net;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the Portal section's section-wide Complete View (ticket 15): a single, uncapped, sortable table
/// listing every Portal item. The by-Portal-item aggregate's row grain already matches what this needs, so no
/// SQL view is required (per ticket 03's resolution - the same reasoning already applied to the Summary and
/// ArcGIS Server Complete Views). Each row's Portal item id links to that item's own live page on Portal (per
/// ticket 05's resolution) - built from the configured <see cref="ReportsSettings.PortalBaseUrl"/>, with no live
/// Portal REST call made at generation time. Each row also links to that item's own Detail Page (ticket 16) - the
/// only place such a link exists.
/// </summary>
public static class PortalCompleteViewBuilder
{
    private const string Title = "Portal » Complete View";
    private const string TableElementId = "portal-complete-view-table";

    /// <summary>
    /// Builds the Portal Complete View page.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="boundaries">This Regeneration Run's resolved year-to-date boundaries.</param>
    /// <param name="generatedAtUtc">The instant this Regeneration Run started, for the page's footer.</param>
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

        var rangeTotalsQuery = new ByPortalItemRangeTotalsQuery(connection);
        var rows = rangeTotalsQuery.GetRangeTotals(boundaries.YearStart, boundaries.Today).ToArray();

        var request = new PageShellRequest
        {
            Title = Title,
            OutputRelativePath = DashboardSidebar.PortalCompleteViewHref,
            ActiveHref = DashboardSidebar.PortalCompleteViewHref,
            BodyHtml = BuildTableBody(rows, settings.PortalBaseUrl),
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = settings.IncludedRoots,
        };

        StaticPageWriter.Write(outputDirectory, request);
    }

    private static string BuildTableBody(IReadOnlyList<PortalItemRangeTotalRow> rows, string portalBaseUrl)
    {
        var rootPrefix = RelativeLinkHelper.ComputeRootPrefix(DashboardSidebar.PortalCompleteViewHref);
        var table = new List<IReadOnlyList<object?>>
        {
            new List<object?>
            {
                "Portal Item", "Hits", "Total Time Taken (s)", "Average Time Taken (s)", "Successful Hits",
                "Failed Hits", "Detail Page",
            },
        };

        foreach (var row in rows)
        {
            var averageTimeTakenSecond = row.Hits > 0 ? row.TotalTimeTakenSecond / row.Hits : 0d;
            var itemHref = PortalItemLiveLink.BuildHref(portalBaseUrl, row.PortalItemId);
            var itemLink = $"<a href=\"{WebUtility.HtmlEncode(itemHref)}\" target=\"_blank\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(row.PortalItemId)}</a>";

            var portalItemCell = new { v = row.PortalItemId, f = itemLink };

            var detailHref = rootPrefix + DashboardSidebar.PortalItemDetailHref(row.PortalItemId);
            var detailLink = $"<a href=\"{WebUtility.HtmlEncode(detailHref)}\">Details</a>";

            table.Add(
            [
                portalItemCell,
                row.Hits,
                Math.Round(row.TotalTimeTakenSecond, 3),
                Math.Round(averageTimeTakenSecond, 3),
                row.SuccessfulHits,
                row.FailedHits,
                detailLink,
            ]);
        }

        return GoogleChartsRenderer.RenderTable(TableElementId, table, TablePaging.Disabled, TableCellContent.Html);
    }
}
