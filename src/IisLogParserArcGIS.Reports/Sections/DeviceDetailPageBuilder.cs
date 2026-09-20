using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds one Detail Page per device of a per-device section (Fieldmaps, ticket 21; Survey123, ticket 22): that
/// device's hits per day, for every device the section's Complete View lists - the same
/// <see cref="DeviceRangeTotalsQuery"/> result, so exactly the rows the Complete View links to get a page here,
/// no more and no fewer. A Detail Page has no sidebar entry and is reachable only by following a link from that
/// Complete View, so - mirroring the Portal item Detail Page (ticket 16) - its All and Last 7 Days variants share
/// one page with an in-page toggle (<see cref="DetailRangeToggle"/>) rather than living at two separate URLs. The
/// chart title carries the device's username (or "Unattributed") and device id, so the operator knows whose
/// device they are looking at.
/// </summary>
public static class DeviceDetailPageBuilder
{
    /// <summary>
    /// Builds every device Detail Page of <paramref name="section"/>.
    /// </summary>
    /// <param name="section">The per-device section to build.</param>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="boundaries">This Regeneration Run's resolved year-to-date boundaries.</param>
    /// <param name="generatedAtUtc">The instant this Regeneration Run started, for every page's footer.</param>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
#pragma warning disable CC0042 // Six independent inputs threaded from RegenerationRun.Run's own "primary seam", matching every other section builder's own justified suppression.
    public static void Build(
        DeviceSection section,
        SqliteConnection connection,
        ReportsSettings settings,
        RegenerationRunBoundaries boundaries,
        DateTimeOffset generatedAtUtc,
        string outputDirectory)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        var devices = AggregateDatabaseSchema.TableExists(connection, section.Table.Name)
            ? new DeviceRangeTotalsQuery(connection, section.Table).GetRangeTotals(boundaries.YearStart, boundaries.Today).ToArray()
            : [];

        var dailyTotalsQuery = new DeviceDetailDailyHitTotalsQuery(connection, section.Table);

        foreach (var device in devices)
        {
            var allRows = dailyTotalsQuery.GetDailyTotals(boundaries.YearStart, boundaries.Today, device.DeviceId).ToArray();

            StaticPageWriter.Write(outputDirectory, BuildPageRequest(section, device, allRows, boundaries, generatedAtUtc, settings));
        }
    }

#pragma warning disable CC0042 // Six independent inputs bundled by no fewer than every other page-request-building helper in this project already threads.
    private static PageShellRequest BuildPageRequest(
        DeviceSection section,
        DeviceRangeTotalRow device,
        IReadOnlyList<DeviceDailyHitTotalRow> allRows,
        RegenerationRunBoundaries boundaries,
        DateTimeOffset generatedAtUtc,
        ReportsSettings settings)
#pragma warning restore CC0042
    {
        var last7Rows = allRows.Where(row => row.LocalDate >= boundaries.Last7DaysStart).ToArray();
        var href = section.DetailHref(device.DeviceId);
        var backHref = RelativeLinkHelper.ComputeRootPrefix(href) + section.CompleteViewHref;
        var titlePrefix = $"{section.Title} » {device.Username ?? "Unattributed"} » {device.DeviceId}";
        var chartId = $"{section.SectionSlug}-device-detail-hits-chart";

        var allChartsHtml = BuildChartHtml($"{chartId}-{DetailRangeToggle.AllSlug}", $"{titlePrefix} » Hits per Day » All", allRows);
        var last7ChartsHtml = BuildChartHtml($"{chartId}-{DetailRangeToggle.Last7DaysSlug}", $"{titlePrefix} » Hits per Day » Last 7 Days", last7Rows);

        return new PageShellRequest
        {
            Title = $"{section.Title} » {device.DeviceId}",
            OutputRelativePath = href,
            ActiveHref = href,
            BodyHtml = DetailRangeToggle.Render(backHref, allChartsHtml, last7ChartsHtml),
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = settings.IncludedRoots,
        };
    }

    private static string BuildChartHtml(string chartId, string chartTitle, IReadOnlyList<DeviceDailyHitTotalRow> rows)
    {
        var dates = rows.Select(row => row.LocalDate).ToArray();
        var values = rows.Select(row => (double?)row.Hits).ToArray();

        return GoogleChartsRenderer.RenderAnnotatedTimeLine(chartId, new AnnotatedTimeLineData(dates, values, "Hits"), chartTitle);
    }
}
