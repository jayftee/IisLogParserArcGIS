using System.Globalization;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds the Summary section: the per-Root total-requests line chart (All and Last 7 Days variants) and the
/// Complete View table (hits, total time taken, average time taken, one row per Root) - the Dashboard's first
/// section (ticket 10), proving the query → embed → render pipeline end-to-end. Sourced directly from the
/// by-Root aggregate; no SQL view is needed at this grain (per ticket 03).
/// </summary>
public static class SummarySectionBuilder
{
    private const string SuccessfulRequestsAllTitle = "Summary » Successful Requests » All";
    private const string SuccessfulRequestsLast7DaysTitle = "Summary » Successful Requests » Last 7 Days";
    private const string CompleteViewTitle = "Summary » Complete View";

    /// <summary>
    /// Builds every page in the Summary section, including the site's <c>index.html</c>, which duplicates the
    /// Summary → Successful Requests → All page's content per ticket 01's landing-page decision.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="boundaries">This Regeneration Run's resolved year-to-date boundaries.</param>
    /// <param name="generatedAtUtc">The instant this Regeneration Run started, for every page's footer.</param>
    /// <param name="outputDirectory">The Dashboard's output root directory.</param>
#pragma warning disable CC0042 // Five independent inputs threaded from RegenerationRun.Run's own "primary seam"; a parameter object would just repackage them without benefit.
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

        var dailyTotalsQuery = new ByRootDailyTotalsQuery(connection);
        var rangeTotalsQuery = new ByRootRangeTotalsQuery(connection);

        var allDailyRows = dailyTotalsQuery.GetDailyTotals(boundaries.YearStart, boundaries.Today, includedRoots).ToArray();
        var last7DaysRows = allDailyRows.Where(row => row.LocalDate >= boundaries.Last7DaysStart).ToArray();
        var rangeTotalsRows = rangeTotalsQuery.GetRangeTotals(boundaries.YearStart, boundaries.Today, includedRoots).ToArray();

        var bodies = new SummaryPageBodies(
            BuildLineChartBody(allDailyRows, includedRoots, SuccessfulRequestsAllTitle),
            BuildLineChartBody(last7DaysRows, includedRoots, SuccessfulRequestsLast7DaysTitle),
            BuildCompleteViewBody(rangeTotalsRows, includedRoots));

        foreach (var request in BuildPageRequests(bodies, includedRoots, generatedAtUtc))
        {
            StaticPageWriter.Write(outputDirectory, request);
        }
    }

    private static IReadOnlyList<PageShellRequest> BuildPageRequests(SummaryPageBodies bodies, IReadOnlyList<string> includedRoots, DateTimeOffset generatedAtUtc)
    {
        var allRequest = new PageShellRequest
        {
            Title = SuccessfulRequestsAllTitle,
            OutputRelativePath = DashboardSidebar.LandingPageHref,
            ActiveHref = DashboardSidebar.LandingPageHref,
            BodyHtml = bodies.AllChart,
            GeneratedAtUtc = generatedAtUtc,
            RequiresGoogleCharts = true,
            IncludedRoots = includedRoots,
        };

        return
        [
            allRequest,
            allRequest with { OutputRelativePath = "index.html" },
            new PageShellRequest
            {
                Title = SuccessfulRequestsLast7DaysTitle,
                OutputRelativePath = DashboardSidebar.SummaryLast7DaysHref,
                ActiveHref = DashboardSidebar.SummaryLast7DaysHref,
                BodyHtml = bodies.Last7DaysChart,
                GeneratedAtUtc = generatedAtUtc,
                RequiresGoogleCharts = true,
                IncludedRoots = includedRoots,
            },
            new PageShellRequest
            {
                Title = CompleteViewTitle,
                OutputRelativePath = DashboardSidebar.SummaryCompleteViewHref,
                ActiveHref = DashboardSidebar.SummaryCompleteViewHref,
                BodyHtml = bodies.CompleteView,
                GeneratedAtUtc = generatedAtUtc,
                RequiresGoogleCharts = true,
                IncludedRoots = includedRoots,
            },
        ];
    }

    private static string BuildLineChartBody(
        IReadOnlyList<ByRootDailyTotalRow> dailyRows, IReadOnlyList<string> includedRoots, string title)
    {
        var dates = dailyRows.Select(row => row.LocalDate).Distinct().Order().ToArray();
        var hitsByDateAndRoot = dailyRows.ToDictionary(row => (row.LocalDate, row.Root), row => row.Hits);

        var columns = new List<ChartColumn> { new("string", "Date") };
        columns.AddRange(includedRoots.Select(root => new ChartColumn("number", root)));

        var dataRows = new List<IReadOnlyList<object?>>();

        foreach (var date in dates)
        {
            var row = new List<object?> { date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };

            foreach (var root in includedRoots)
            {
                row.Add(hitsByDateAndRoot.TryGetValue((date, root), out var hits) ? hits : null);
            }

            dataRows.Add(row);
        }

        return GoogleChartsRenderer.RenderLineChart("summary-total-requests-chart", new LineChartData(columns, dataRows), title);
    }

    private static string BuildCompleteViewBody(IReadOnlyList<ByRootRangeTotalRow> rangeTotalsRows, IReadOnlyList<string> includedRoots)
    {
        var totalsByRoot = rangeTotalsRows.ToDictionary(row => row.Root, row => row);

        var table = new List<IReadOnlyList<object?>>
        {
            new List<object?> { "Root", "Hits", "Total Time Taken (s)", "Average Time Taken (s)" },
        };

        foreach (var root in includedRoots.Order(StringComparer.Ordinal))
        {
            var hits = 0;
            var totalTimeTakenSecond = 0d;

            if (totalsByRoot.TryGetValue(root, out var found))
            {
                hits = found.Hits;
                totalTimeTakenSecond = found.TotalTimeTakenSecond;
            }

            var averageTimeTakenSecond = hits > 0 ? totalTimeTakenSecond / hits : 0d;

            table.Add([root, hits, Math.Round(totalTimeTakenSecond, 3), Math.Round(averageTimeTakenSecond, 3)]);
        }

        return GoogleChartsRenderer.RenderTable("summary-complete-view-table", table, title: CompleteViewTitle);
    }

    private sealed record SummaryPageBodies(string AllChart, string Last7DaysChart, string CompleteView);
}
