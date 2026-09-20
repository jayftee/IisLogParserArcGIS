using System.Net;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the ArcGIS Server section's section-wide Complete View (ticket 12): a single, uncapped, sortable table
/// listing every folder/service/type combination across every Included Root, site as a column. The by-ArcGIS-
/// Server-service aggregate's row grain already matches what this needs, so no SQL view is required (per ticket
/// 03's resolution - the same reasoning already applied to the Summary section's Complete View). Each row's
/// Service Name links to that service's own live REST endpoint on ArcGIS Server (mirroring the Portal Complete
/// View's Portal Item column), and its own "Detail Page" column links to that service's own Detail Page
/// (ticket 13) - the only place such a link exists.
/// </summary>
public static class ArcGisServerCompleteViewBuilder
{
    private const string Title = "ArcGIS Server » Complete View";
    private const string TableElementId = "arcgis-server-complete-view-table";

    /// <summary>
    /// Builds the ArcGIS Server Complete View page.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="boundaries">This Regeneration Run's resolved year-to-date boundaries.</param>
    /// <param name="generatedAtUtc">The instant this Regeneration Run started, for the page's footer.</param>
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

        var includedRoots = settings.IncludedRoots;
        var rangeTotalsQuery = new ByArcGisServiceRangeTotalsQuery(connection);
        var rows = rangeTotalsQuery.GetRangeTotals(boundaries.YearStart, boundaries.Today, includedRoots).ToArray();

        var request = new PageShellRequest
        {
            Title = Title,
            OutputRelativePath = DashboardSidebar.ArcGisServerCompleteViewHref,
            ActiveHref = DashboardSidebar.ArcGisServerCompleteViewHref,
            BodyHtml = BuildTableBody(rows, settings.ArcGisServerBaseUrl),
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = includedRoots,
        };

        StaticPageWriter.Write(outputDirectory, request);
    }

    private static string BuildTableBody(IReadOnlyList<ArcGisServiceRangeTotalRow> rows, string arcGisServerBaseUrl)
    {
        var rootPrefix = RelativeLinkHelper.ComputeRootPrefix(DashboardSidebar.ArcGisServerCompleteViewHref);
        var table = new List<IReadOnlyList<object?>>
        {
            new List<object?>
            {
                "Site", "Folder", "Service Name", "Service Type", "Hits", "Total Time Taken (s)",
                "Average Time Taken (s)", "Successful Hits", "Failed Hits", "Detail Page",
            },
        };

        foreach (var row in rows)
        {
            var averageTimeTakenSecond = row.Hits > 0 ? row.TotalTimeTakenSecond / row.Hits : 0d;

            var serviceHref = ArcGisServiceLiveLink.BuildHref(arcGisServerBaseUrl, row.Site, row.Folder, row.ServiceName, row.ServiceType);
            var serviceNameLink = $"<a href=\"{WebUtility.HtmlEncode(serviceHref)}\" target=\"_blank\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(row.ServiceName)}</a>";

            // {v, f}: arrayToDataTable's own formatted-value shape - sorts on the plain name (v), displays the link (f).
            var serviceNameCell = new { v = row.ServiceName, f = serviceNameLink };

            var detailHref = rootPrefix + DashboardSidebar.ArcGisServiceDetailHref(row.Site, row.Folder, row.ServiceName, row.ServiceType);
            var detailLink = $"<a href=\"{WebUtility.HtmlEncode(detailHref)}\">Details</a>";

            table.Add(
            [
                row.Site,
                row.Folder ?? string.Empty,
                serviceNameCell,
                row.ServiceType,
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
