using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies the Portal section (ticket 14) end-to-end against the real, harvested fixture database (ticket 09),
/// per spec.md's "primary seam" testing decision: drive <see cref="RegenerationRun.Run"/> and assert against
/// the files it writes, rather than against internal query/render classes directly.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class PortalSectionTests
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes both the All (year-to-date) and Last 7 Days ranges fall entirely within real harvested data.
    private static readonly DateOnly _today = new(2026, 9, 10);
    private static readonly DateOnly _yearStart = new(2026, 1, 1);

    private static readonly string[] _metricSlugs =
    [
        DashboardSidebar.PortalSuccessfulRequestsSlug,
        DashboardSidebar.PortalFailedRequestsSlug,
        DashboardSidebar.PortalLeaderboardSuccessfulHitsSlug,
        DashboardSidebar.PortalLeaderboardFailedHitsSlug,
    ];

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public PortalSectionTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Run_WritesEveryPortalMetricPage()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        foreach (var metricSlug in _metricSlugs)
        {
            var allPath = Path.Combine(outputDirectory.Path, "portal", metricSlug, "all.html");
            var last7Path = Path.Combine(outputDirectory.Path, "portal", metricSlug, "last-7-days.html");

            Assert.True(File.Exists(allPath), $"Missing {allPath}");
            Assert.True(File.Exists(last7Path), $"Missing {last7Path}");
        }
    }

    [Fact]
    public void Run_WithNoIncludedRoots_StillWritesEveryPortalMetricPage()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettingsWithNoIncludedRoots();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        foreach (var metricSlug in _metricSlugs)
        {
            var allPath = Path.Combine(outputDirectory.Path, "portal", metricSlug, "all.html");

            Assert.True(File.Exists(allPath), $"Missing {allPath}");
        }
    }

    [Fact]
    public void Run_SidebarOnTheLandingPage_ListsThePortalSectionUnconditionally()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettingsWithNoIncludedRoots();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "index.html"));

        Assert.Contains(">Portal<", html, StringComparison.Ordinal);
        Assert.Contains($"portal/{DashboardSidebar.PortalLeaderboardSuccessfulHitsSlug}/all.html", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_SuccessfulRequestsAll_MatchesAnIndependentlyComputedPerDayOracle_NeverCumulative()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(
            outputDirectory.Path, "portal", DashboardSidebar.PortalSuccessfulRequestsSlug, "all.html"));

        var oracleRows = connection.Query<(string LocalDate, int SuccessfulHits)>(
            """
            SELECT local_date AS LocalDate, SUM(successful_hits) AS SuccessfulHits
            FROM aggregated_by_portal_item
            WHERE local_date BETWEEN @Start AND @End
            GROUP BY local_date
            """,
            new { Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleRows);

        foreach (var (localDateText, successfulHits) in oracleRows)
        {
            var localDate = DateOnly.Parse(localDateText, CultureInfo.InvariantCulture);
            var expectedLiteral = $"[new Date({localDate.Year}, {localDate.Month - 1}, {localDate.Day}), {successfulHits}]";

            Assert.Contains(expectedLiteral, html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Run_LeaderboardSuccessfulHitsAll_OrdersDescendingWithNoManufacturedTieBreak()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(
            outputDirectory.Path, "portal", DashboardSidebar.PortalLeaderboardSuccessfulHitsSlug, "all.html"));

        var oracleHits = connection.Query<int>(
            """
            SELECT SUM(successful_hits) AS Hits
            FROM aggregated_by_portal_item
            WHERE local_date BETWEEN @Start AND @End
            GROUP BY portal_item_id
            HAVING SUM(successful_hits) > 0
            ORDER BY Hits DESC
            LIMIT 50
            """,
            new { Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleHits);

        var chartValues = ExtractBarChartValues(html);

        Assert.Equal(oracleHits, chartValues);
    }

    [Fact]
    public void Run_LeaderboardFailedHitsAll_OrdersDescendingWithNoManufacturedTieBreak()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(
            outputDirectory.Path, "portal", DashboardSidebar.PortalLeaderboardFailedHitsSlug, "all.html"));

        var oracleHits = connection.Query<int>(
            """
            SELECT SUM(failed_hits) AS Hits
            FROM aggregated_by_portal_item
            WHERE local_date BETWEEN @Start AND @End
            GROUP BY portal_item_id
            HAVING SUM(failed_hits) > 0
            ORDER BY Hits DESC
            LIMIT 50
            """,
            new { Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleHits);

        var chartValues = ExtractBarChartValues(html);

        Assert.Equal(oracleHits, chartValues);
    }

    private static int[] ExtractBarChartValues(string html)
    {
        var match = Regex.Match(html, @"arrayToDataTable\((?<json>\[.*?\])\);", RegexOptions.Singleline);
        Assert.True(match.Success, "Could not find the embedded BarChart data table in the page.");

        var capturedJson = match.Groups["json"].Value;
        var document = JsonDocument.Parse(capturedJson);
        var root = document.RootElement;
        var rows = root.EnumerateArray().Skip(1);

        return rows.Select(row => row[1].GetInt32()).ToArray();
    }

    private static FakeTimeProvider CreateTimeProvider()
    {
        return new FakeTimeProvider(new DateTimeOffset(_today, TimeOnly.MinValue, TimeSpan.Zero));
    }

    private static ReportsSettings CreateSettings()
    {
        return new ReportsSettings
        {
            LocalTimeZone = "UTC",
            IncludedRoots =
            [
                "arcgis", "charon", "deimos", "europa", "galatea", "iapetus", "kerberos",
                "mimas", "namaka", "oberon", "phobos", "rhea", "titan", "umbriel",
            ],
            PortalBaseUrl = "portal.example.com",
            OutputDirectory = "Dashboard",
        };
    }

    private static ReportsSettings CreateSettingsWithNoIncludedRoots()
    {
        return new ReportsSettings
        {
            LocalTimeZone = "UTC",
            IncludedRoots = [],
            PortalBaseUrl = "portal.example.com",
            OutputDirectory = "Dashboard",
        };
    }
}
