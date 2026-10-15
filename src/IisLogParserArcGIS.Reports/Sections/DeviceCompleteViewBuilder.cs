using System.Globalization;
using System.Net;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds a per-device section's Complete View (Fieldmaps, ticket 21; Survey123, ticket 22): a single, uncapped,
/// sortable table listing every device - attributed or not - with all days merged (calendar-year-to-date). The
/// table's own (date, device) grain already matches what this needs, so no SQL view is required (per ticket 03).
/// Each row also links to that device's own Detail Page - the only place such a link exists. Last Seen is
/// rendered as ISO <c>yyyy-MM-dd</c> text, which sorts chronologically as text. Paged in the browser at 1000
/// rows/page, with a full, unpaged copy of every row available as a sibling <c>.tsv</c> file (reports ticket 30).
/// </summary>
public static class DeviceCompleteViewBuilder
{
    private const string IsoDateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Builds the section's Complete View page.
    /// </summary>
    /// <param name="section">The per-device section to build.</param>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="boundaries">This Regeneration Run's resolved year-to-date boundaries.</param>
    /// <param name="generatedAtUtc">The instant this Regeneration Run started, for the page's footer.</param>
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

        var rows = AggregateDatabaseSchema.TableExists(connection, section.Table.Name)
            ? new DeviceRangeTotalsQuery(connection, section.Table).GetRangeTotals(boundaries.YearStart, boundaries.Today).ToArray()
            : [];
        var table = BuildTable(section, rows);

        var title = $"{section.Title} » Complete View";

        var bodyHtml = CompleteViewBody.Build(
            outputDirectory, section.CompleteViewHref, $"{section.SectionSlug}-complete-view-table", table, trailingColumnsToDrop: 1, cellContent: TableCellContent.Html, title: title);

        var request = new PageShellRequest
        {
            Title = title,
            OutputRelativePath = section.CompleteViewHref,
            ActiveHref = section.CompleteViewHref,
            BodyHtml = bodyHtml,
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = settings.IncludedRoots,
        };

        StaticPageWriter.Write(outputDirectory, request);
    }

    private static List<IReadOnlyList<object?>> BuildTable(DeviceSection section, IReadOnlyList<DeviceRangeTotalRow> rows)
    {
        var rootPrefix = RelativeLinkHelper.ComputeRootPrefix(section.CompleteViewHref);
        var table = new List<IReadOnlyList<object?>>
        {
            new List<object?>
            {
                "Username", "Device ID", "Hits", "Total Time Taken (s)", "Average Time Taken (s)", "Last Seen", "Detail Page",
            },
        };

        foreach (var row in rows)
        {
            var averageTimeTakenSecond = row.Hits > 0 ? row.TotalTimeTakenSecond / row.Hits : 0d;
            var detailHref = rootPrefix + section.DetailHref(row.DeviceId);
            var detailLink = $"<a href=\"{WebUtility.HtmlEncode(detailHref)}\">Details</a>";

            table.Add(
            [
                HtmlSafeTableCell.Create(row.Username),
                HtmlSafeTableCell.Create(row.DeviceId),
                row.Hits,
                Math.Round(row.TotalTimeTakenSecond, 3),
                Math.Round(averageTimeTakenSecond, 3),
                row.LastSeen.ToString(IsoDateFormat, CultureInfo.InvariantCulture),
                detailLink,
            ]);
        }

        return table;
    }
}
