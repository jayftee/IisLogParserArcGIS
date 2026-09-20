using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies the Summary section (ticket 10) end-to-end against the real, harvested fixture database (ticket 09),
/// per spec.md's "primary seam" testing decision: drive <see cref="RegenerationRun.Run"/> and assert against the
/// files it writes, rather than against internal query/render classes directly.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class SummarySectionTests
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes both the All (year-to-date) and Last 7 Days ranges fall entirely within real harvested data.
    private static readonly DateOnly _today = new(2026, 9, 10);
    private static readonly DateOnly _yearStart = new(2026, 1, 1);

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public SummarySectionTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Run_AgainstTheHarvestedDatabase_WritesEveryExpectedSummaryFile()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "index.html")));
        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "summary", "successful-requests", "all.html")));
        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "summary", "successful-requests", "last-7-days.html")));
        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "summary", "complete-view.html")));
        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "assets", "site.css")));
    }

    [Fact]
    public void Run_IndexHtml_ShowsTheSameContentAsTheSummaryAllPage()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var indexHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, "index.html"));
        var allHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, "summary", "successful-requests", "all.html"));

        Assert.Equal(ExtractMainContent(allHtml), ExtractMainContent(indexHtml));
    }

    [Fact]
    public void Run_CompleteView_TotalsMatchAnIndependentlyComputedOracle()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var completeViewHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, "summary", "complete-view.html"));

        foreach (var root in settings.IncludedRoots)
        {
            var expectedHits = connection.QuerySingle<int>(
                """
                SELECT COALESCE(SUM(hits), 0) FROM aggregated_by_root
                WHERE root = @Root AND local_date BETWEEN @Start AND @End
                """,
                new { Root = root, Start = _yearStart, End = _today });

            Assert.Contains($"\"{root}\",{expectedHits},", completeViewHtml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Run_SuccessfulRequestsAll_ContainsEveryRealIncludedRootAsASeries()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var allHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, "summary", "successful-requests", "all.html"));

        foreach (var root in settings.IncludedRoots)
        {
            Assert.Contains($"\"{root}\"", allHtml, StringComparison.Ordinal);
        }
    }

    private static string ExtractMainContent(string html)
    {
        const string StartMarker = "<main class=\"dashboard-content\">";
        const string EndMarker = "</main>";

        var startIndex = html.IndexOf(StartMarker, StringComparison.Ordinal) + StartMarker.Length;
        var endIndex = html.IndexOf(EndMarker, startIndex, StringComparison.Ordinal);

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
