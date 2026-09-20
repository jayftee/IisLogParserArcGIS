using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds a per-device section's Top-50 Leaderboard pages (Fieldmaps, ticket 21; Survey123, ticket 22): the 50
/// devices with the most hits, as a bar chart with one bar per device (not per user), for All
/// (calendar-year-to-date) and Last 7 Days. The section renders unconditionally, regardless of the Included Root
/// allow-list - the by-device tables have no Root column. A database that predates the table renders empty-data
/// cards rather than failing. Every figure is computed on the fly against the aggregate database each run; no
/// rollup table is introduced (per ticket 03).
/// </summary>
public static class DeviceLeaderboardSectionBuilder
{
    /// <summary>
    /// Builds every page in the section's Leaderboard.
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

        var tableExists = AggregateDatabaseSchema.TableExists(connection, section.Table.Name);
        var query = new DeviceTopHitsQuery(connection, section.Table);

        var ranges = new[]
        {
            (Range: DashboardDateRange.All, Label: "All", Start: boundaries.YearStart),
            (Range: DashboardDateRange.Last7Days, Label: "Last 7 Days", Start: boundaries.Last7DaysStart),
        };

        foreach (var (range, label, start) in ranges)
        {
            var rows = tableExists ? query.GetTop50(start, boundaries.Today).ToArray() : [];
            var href = section.LeaderboardHitsHref(range);
            var title = $"{section.Title} » Leaderboard » Hits » {label}";

            StaticPageWriter.Write(
                outputDirectory,
                new PageShellRequest
                {
                    Title = title,
                    OutputRelativePath = href,
                    ActiveHref = href,
                    BodyHtml = BuildLeaderboardBody(section, rows, title),
                    GeneratedAtUtc = generatedAtUtc,
                    RequiresGoogleCharts = true,
                    IncludedRoots = settings.IncludedRoots,
                });
        }
    }

    /// <summary>
    /// Builds the Leaderboard's chart body. Rows are rendered in the order the query already ranked them - no
    /// secondary sort (ticket 06). Each bar is labeled per <see cref="DeviceLabel"/>.
    /// </summary>
    private static string BuildLeaderboardBody(DeviceSection section, IReadOnlyList<DeviceHitsLeaderboardRow> rows, string title)
    {
        var entries = rows.Select(row => new BarChartEntry(DeviceLabel.Format(row.Username, row.DeviceId), row.Hits)).ToArray();

        return GoogleChartsRenderer.RenderBarChart($"{section.SectionSlug}-leaderboard-hits-chart", entries, "Hits", title);
    }
}
