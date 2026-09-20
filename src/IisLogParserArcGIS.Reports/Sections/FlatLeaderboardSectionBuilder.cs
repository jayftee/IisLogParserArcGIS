using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the four flat, standalone Leaderboard pages (ticket 17): forwarded-for IP, referer, URI, and user
/// agent, each with All and Last 7 Days variants - 8 pages total. Each page lists up to the top 500 values by
/// summed hits, sourced directly from its own existing flat aggregate table - no new SQL view needed, unlike the
/// two uncapped section-wide Complete Views (tickets 12, 15), which page an unbounded row count instead of
/// capping it in SQL. Rendered as a sortable <c>google.visualization.Table</c> (per this ticket's AC), not a
/// <c>BarChart</c> like the Portal/ArcGIS Server Top-50 Leaderboards.
/// </summary>
public static class FlatLeaderboardSectionBuilder
{
    private const string ForwardedForIpTableId = "leaderboard-forwarded-for-ip-table";
    private const string RefererTableId = "leaderboard-referer-table";
    private const string UriTableId = "leaderboard-uri-table";
    private const string UserAgentTableId = "leaderboard-user-agent-table";

    /// <summary>
    /// Builds every page in the flat Leaderboard section.
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

        var metrics = BuildDimensions(connection).Select(dimension => BuildMetricPageContent(dimension, boundaries));

        foreach (var request in metrics.SelectMany(metric => BuildMetricPageRequests(metric, settings.IncludedRoots, generatedAtUtc)))
        {
            StaticPageWriter.Write(outputDirectory, request);
        }
    }

    private static IReadOnlyList<FlatLeaderboardDimension> BuildDimensions(SqliteConnection connection)
    {
        var forwardedForIpQuery = new ByForwardedForIpTopHitsQuery(connection);
        var refererQuery = new ByRefererTopHitsQuery(connection);
        var uriQuery = new ByUriTopHitsQuery(connection);
        var userAgentQuery = new ByUserAgentTopHitsQuery(connection);

        return
        [
            new FlatLeaderboardDimension(
                DashboardSidebar.LeaderboardForwardedForIpSlug,
                "Leaderboard » Forwarded-For IP",
                "Forwarded-For IP",
                ForwardedForIpTableId,
                forwardedForIpQuery.GetTop500),
            new FlatLeaderboardDimension(
                DashboardSidebar.LeaderboardRefererSlug,
                "Leaderboard » Referer",
                "Referer",
                RefererTableId,
                refererQuery.GetTop500),
            new FlatLeaderboardDimension(
                DashboardSidebar.LeaderboardUriSlug,
                "Leaderboard » URI",
                "URI",
                UriTableId,
                uriQuery.GetTop500),
            new FlatLeaderboardDimension(
                DashboardSidebar.LeaderboardUserAgentSlug,
                "Leaderboard » User Agent",
                "User Agent",
                UserAgentTableId,
                userAgentQuery.GetTop500),
        ];
    }

    private static MetricPageContent BuildMetricPageContent(FlatLeaderboardDimension dimension, RegenerationRunBoundaries boundaries)
    {
        var allRows = dimension.GetTop500(boundaries.YearStart, boundaries.Today).ToArray();
        var last7Rows = dimension.GetTop500(boundaries.Last7DaysStart, boundaries.Today).ToArray();

        return new MetricPageContent(
            dimension.Slug,
            dimension.TitlePrefix,
            BuildTableBody(allRows, dimension),
            BuildTableBody(last7Rows, dimension));
    }

    private static IEnumerable<PageShellRequest> BuildMetricPageRequests(MetricPageContent content, IReadOnlyList<string> includedRoots, DateTimeOffset generatedAtUtc)
    {
        var allHref = DashboardSidebar.LeaderboardPageHref(content.MetricSlug, DashboardDateRange.All);
        var last7Href = DashboardSidebar.LeaderboardPageHref(content.MetricSlug, DashboardDateRange.Last7Days);

        yield return new PageShellRequest
        {
            Title = $"{content.TitlePrefix} » All",
            OutputRelativePath = allHref,
            ActiveHref = allHref,
            BodyHtml = content.AllBody,
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = includedRoots,
        };

        yield return new PageShellRequest
        {
            Title = $"{content.TitlePrefix} » Last 7 Days",
            OutputRelativePath = last7Href,
            ActiveHref = last7Href,
            BodyHtml = content.Last7Body,
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = includedRoots,
        };
    }

    private static string BuildTableBody(IReadOnlyList<FlatLeaderboardRow> rows, FlatLeaderboardDimension dimension)
    {
        var table = new List<IReadOnlyList<object?>>
        {
            new List<object?> { dimension.ColumnLabel, "Hits", "Total Time Taken (s)", "Average Time Taken (s)" },
        };

        foreach (var row in rows)
        {
            var averageTimeTakenSecond = row.Hits > 0 ? row.TotalTimeTakenSecond / row.Hits : 0d;

            table.Add(
            [
                row.Value,
                row.Hits,
                Math.Round(row.TotalTimeTakenSecond, 3),
                Math.Round(averageTimeTakenSecond, 3),
            ]);
        }

        return GoogleChartsRenderer.RenderTable(dimension.TableElementId, table, TablePaging.Enabled);
    }

#pragma warning disable CC0042 // Five independent per-dimension values bundled so every helper below stays under three arguments; splitting further would just repackage them without benefit.
    private sealed record FlatLeaderboardDimension(
        string Slug,
        string TitlePrefix,
        string ColumnLabel,
        string TableElementId,
        Func<DateOnly, DateOnly, IEnumerable<FlatLeaderboardRow>> GetTop500);
#pragma warning restore CC0042

#pragma warning disable CC0042 // Four independent per-metric values bundled, matching the other section builders' identically-shaped MetricPageContent.
    private sealed record MetricPageContent(string MetricSlug, string TitlePrefix, string AllBody, string Last7Body);
#pragma warning restore CC0042
}
