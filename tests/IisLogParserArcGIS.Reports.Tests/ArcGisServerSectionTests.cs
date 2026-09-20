using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies the ArcGIS Server section (ticket 11) end-to-end against the real, harvested fixture database
/// (ticket 09), per spec.md's "primary seam" testing decision: drive <see cref="RegenerationRun.Run"/> and
/// assert against the files it writes, rather than against internal query/render classes directly.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class ArcGisServerSectionTests
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes both the All (year-to-date) and Last 7 Days ranges fall entirely within real harvested data.
    private static readonly DateOnly _today = new(2026, 9, 10);
    private static readonly DateOnly _yearStart = new(2026, 1, 1);

    private static readonly string[] _metricSlugs =
    [
        DashboardSidebar.ArcGisServerSuccessfulRequestsSlug,
        DashboardSidebar.ArcGisServerFailedRequestsSlug,
        DashboardSidebar.ArcGisServerAverageTimeSuccessfulSlug,
        DashboardSidebar.ArcGisServerAverageTimeFailedSlug,
        DashboardSidebar.ArcGisServerLeaderboardSuccessfulHitsSlug,
        DashboardSidebar.ArcGisServerLeaderboardFailedHitsSlug,
        DashboardSidebar.ArcGisServerLeaderboardAverageTimeSuccessfulSlug,
        DashboardSidebar.ArcGisServerLeaderboardAverageTimeFailedSlug,
    ];

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public ArcGisServerSectionTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Run_WritesEveryMetricPageForEveryIncludedRoot()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        foreach (var site in settings.IncludedRoots)
        {
            foreach (var metricSlug in _metricSlugs)
            {
                var allPath = Path.Combine(outputDirectory.Path, "arcgis-server", site, metricSlug, "all.html");
                var last7Path = Path.Combine(outputDirectory.Path, "arcgis-server", site, metricSlug, "last-7-days.html");

                Assert.True(File.Exists(allPath), $"Missing {allPath}");
                Assert.True(File.Exists(last7Path), $"Missing {last7Path}");
            }
        }
    }

    [Fact]
    public void Run_DoesNotWriteAnyPageForTheUnlistedProxyRoot()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        Assert.False(Directory.Exists(Path.Combine(outputDirectory.Path, "arcgis-server", "proxy")));
    }

    [Fact]
    public void Run_SidebarOnTheLandingPage_ListsExactlyTheFourteenIncludedRootsUnderArcGisServer()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "index.html"));
        var arcGisServerHtml = ExtractSection(html, "ArcGIS Server", "Leaderboard");

        foreach (var root in settings.IncludedRoots)
        {
            Assert.Contains($"<summary>{root}</summary>", arcGisServerHtml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("<summary>proxy</summary>", arcGisServerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_SuccessfulRequestsAll_MatchesAnIndependentlyComputedPerDayOracle_NeverCumulative()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        const string Site = "mimas";

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(
            outputDirectory.Path, "arcgis-server", Site, DashboardSidebar.ArcGisServerSuccessfulRequestsSlug, "all.html"));

        var oracleRows = connection.Query<(string LocalDate, int SuccessfulHits)>(
            """
            SELECT local_date AS LocalDate, SUM(successful_hits) AS SuccessfulHits
            FROM aggregated_by_arcgis_service
            WHERE site = @Site AND local_date BETWEEN @Start AND @End
            GROUP BY local_date
            """,
            new { Site, Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleRows);

        foreach (var (localDateText, successfulHits) in oracleRows)
        {
            var localDate = DateOnly.Parse(localDateText, CultureInfo.InvariantCulture);
            var expectedLiteral = $"[new Date({localDate.Year}, {localDate.Month - 1}, {localDate.Day}), {successfulHits}]";

            Assert.Contains(expectedLiteral, html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Run_AverageTimeSuccessfulAll_RecomputesPerDayFromTimeTakenRatherThanReusingHitCountRows()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        const string Site = "mimas";

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(
            outputDirectory.Path, "arcgis-server", Site, DashboardSidebar.ArcGisServerAverageTimeSuccessfulSlug, "all.html"));

        var oracleRows = connection.Query<(string LocalDate, double SuccessfulTimeTakenSecond, int SuccessfulHits)>(
            """
            SELECT local_date AS LocalDate, SUM(successful_time_taken_second) AS SuccessfulTimeTakenSecond, SUM(successful_hits) AS SuccessfulHits
            FROM aggregated_by_arcgis_service
            WHERE site = @Site AND local_date BETWEEN @Start AND @End
            GROUP BY local_date
            HAVING SUM(successful_hits) > 0
            """,
            new { Site, Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleRows);

        foreach (var (localDateText, totalTimeTakenSecond, successfulHits) in oracleRows)
        {
            var localDate = DateOnly.Parse(localDateText, CultureInfo.InvariantCulture);
            var expectedAverage = Math.Round(totalTimeTakenSecond / successfulHits, 3);
            var expectedLiteral = $"[new Date({localDate.Year}, {localDate.Month - 1}, {localDate.Day}), {expectedAverage.ToString(CultureInfo.InvariantCulture)}]";

            Assert.Contains(expectedLiteral, html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Run_LeaderboardSuccessfulHitsAll_OrdersDescendingWithNoManufacturedTieBreak()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        const string Site = "arcgis";

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(
            outputDirectory.Path, "arcgis-server", Site, DashboardSidebar.ArcGisServerLeaderboardSuccessfulHitsSlug, "all.html"));

        var oracleHits = connection.Query<int>(
            """
            SELECT SUM(successful_hits) AS Hits
            FROM aggregated_by_arcgis_service
            WHERE site = @Site AND local_date BETWEEN @Start AND @End
            GROUP BY folder, service_name, service_type
            ORDER BY Hits DESC
            LIMIT 50
            """,
            new { Site, Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleHits);

        var chartValues = ExtractBarChartValues(html);

        Assert.Equal(oracleHits, chartValues);
    }

    [Fact]
    public void Run_LeaderboardWithFewerThan50Services_RendersOnlyAsManyBarsAsExist_NoPlaceholders()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertArcGisServiceRows(
            LowServiceRow("firemap-edgecase", _today),
            LowServiceRow("watermap-edgecase", _today),
            LowServiceRow("landmap-edgecase", _today));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        var oracleServiceCount = connection.QuerySingle<int>(
            """
            SELECT COUNT(*) FROM (
                SELECT folder, service_name, service_type
                FROM aggregated_by_arcgis_service
                WHERE site = 'rhea' AND local_date BETWEEN @Start AND @End
                GROUP BY folder, service_name, service_type
            )
            """,
            new { Start = _yearStart, End = _today });

        Assert.True(oracleServiceCount < 50, "This test expects rhea to have fewer than 50 services, to exercise the under-full Leaderboard case.");

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(
            outputDirectory.Path, "arcgis-server", "rhea", DashboardSidebar.ArcGisServerLeaderboardSuccessfulHitsSlug, "all.html"));

        var chartValues = ExtractBarChartValues(html);

        Assert.Equal(oracleServiceCount, chartValues.Length);
    }

    private static ByArcGisServiceAggregateRow LowServiceRow(string serviceName, DateOnly localDate) => new()
    {
        LocalDate = localDate,
        Site = "rhea",
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = 1.0,
        FailedTimeTakenSecond = 0,
        Hits = 1,
        SuccessfulHits = 1,
        FailedHits = 0,
    };

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

    private static string ExtractSection(string html, string sectionTitle, string nextSiblingSectionTitle)
    {
        var startMarker = $">{sectionTitle}</summary>";
        var endMarker = $">{nextSiblingSectionTitle}</summary>";
        var startIndex = html.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Could not find sidebar section '{sectionTitle}'.");

        var endIndex = html.IndexOf(endMarker, startIndex + startMarker.Length, StringComparison.Ordinal);
        Assert.True(endIndex >= 0, $"Could not find sidebar section '{nextSiblingSectionTitle}'.");

        return html[startIndex..endIndex];
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
}
