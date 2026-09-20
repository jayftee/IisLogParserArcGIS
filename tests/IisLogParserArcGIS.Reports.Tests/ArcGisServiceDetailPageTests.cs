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
/// Verifies the ArcGIS Server service Detail Page (ticket 13) end-to-end against the real, harvested fixture
/// database (ticket 09), per spec.md's "primary seam" testing decision: drive <see cref="RegenerationRun.Run"/>
/// and assert against the files it writes, rather than against internal query/render classes directly.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class ArcGisServiceDetailPageTests
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes the All (year-to-date) range fall entirely within real harvested data.
    private static readonly DateOnly _today = new(2026, 9, 10);
    private static readonly DateOnly _yearStart = new(2026, 1, 1);

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public ArcGisServiceDetailPageTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Run_WritesADetailPageForEveryServiceTheCompleteViewLists()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var oracleServices = connection.Query<(string Site, string? Folder, string ServiceName, string ServiceType)>(
            """
            SELECT site AS Site, folder AS Folder, service_name AS ServiceName, service_type AS ServiceType
            FROM aggregated_by_arcgis_service
            WHERE site IN @IncludedRoots AND local_date BETWEEN @Start AND @End
            GROUP BY site, folder, service_name, service_type
            """,
            new { IncludedRoots = settings.IncludedRoots, Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleServices);

        foreach (var service in oracleServices)
        {
            var href = DashboardSidebar.ArcGisServiceDetailHref(service.Site, service.Folder, service.ServiceName, service.ServiceType);
            var fullPath = Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(fullPath), $"Expected a Detail Page at '{href}' for {service.Site}/{service.Folder}/{service.ServiceName}/{service.ServiceType}.");
        }
    }

    [Fact]
    public void Run_DetailPage_HasNoSidebarEntry_ReachableOnlyFromTheCompleteView()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var oracleService = connection.QuerySingle<(string Site, string? Folder, string ServiceName, string ServiceType)>(
            """
            SELECT site AS Site, folder AS Folder, service_name AS ServiceName, service_type AS ServiceType
            FROM aggregated_by_arcgis_service
            WHERE site IN @IncludedRoots AND local_date BETWEEN @Start AND @End
            GROUP BY site, folder, service_name, service_type
            LIMIT 1
            """,
            new { IncludedRoots = settings.IncludedRoots, Start = _yearStart, End = _today });

        var href = DashboardSidebar.ArcGisServiceDetailHref(oracleService.Site, oracleService.Folder, oracleService.ServiceName, oracleService.ServiceType);
        var completeViewHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, "arcgis-server", "complete-view.html"));
        var navHtml = ExtractSidebarNav(completeViewHtml);

        Assert.DoesNotContain(href, navHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_DetailPage_EmbedsBothRangesForAnInPageToggle_NoSeparatePages()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertArcGisServiceRows(ServiceRow("toggle-service", "titan", _today, hits: 3));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var href = DashboardSidebar.ArcGisServiceDetailHref("titan", null, "toggle-service", "MapServer");
        var fullPath = Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar));
        var html = File.ReadAllText(fullPath);

        Assert.Contains("data-range=\"all\"", html, StringComparison.Ordinal);
        Assert.Contains("data-range=\"last-7-days\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"detail-range-template\"", html, StringComparison.Ordinal);
        Assert.Contains("range-toggle-button", html, StringComparison.Ordinal);

        var serviceDirectory = Path.GetDirectoryName(fullPath)!;
        Assert.Equal(new[] { fullPath }, Directory.GetFiles(serviceDirectory, "*.html"));
    }

    [Fact]
    public void Run_ServiceNameWithFilesystemIllegalCharacters_WritesASanitizedDetailPageInstead()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertArcGisServiceRows(ServiceRow("weird*service?", "titan", _today, hits: 3));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var href = DashboardSidebar.ArcGisServiceDetailHref("titan", null, "weird*service?", "MapServer");
        var fullPath = Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar));

        Assert.DoesNotContain('*', href);
        Assert.DoesNotContain('?', href);
        Assert.True(File.Exists(fullPath), $"Expected a sanitized Detail Page at '{href}'.");
    }

    [Fact]
    public void Run_DetailPage_ChartsSpanOnlyTheServicesRealDateRange_NoZeroPaddingBetweenSparseDates()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        var earlyDate = _yearStart.AddDays(10);
        var lateDate = _today.AddDays(-10);
        var middleDate = _yearStart.AddDays(150);
        workingCopy.InsertArcGisServiceRows(
            ServiceRow("sparse-service", "titan", earlyDate, hits: 2),
            ServiceRow("sparse-service", "titan", lateDate, hits: 5));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var href = DashboardSidebar.ArcGisServiceDetailHref("titan", null, "sparse-service", "MapServer");
        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar)));
        var chartScript = ExtractScriptContaining(html, "arcgis-service-detail-successful-requests-chart-all");

        Assert.Contains(DateLiteral(earlyDate), chartScript, StringComparison.Ordinal);
        Assert.Contains(DateLiteral(lateDate), chartScript, StringComparison.Ordinal);
        Assert.DoesNotContain(DateLiteral(middleDate), chartScript, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(chartScript, "new Date("));
    }

    private static string DateLiteral(DateOnly date) => $"new Date({date.Year}, {date.Month - 1}, {date.Day})";

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;

        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static string ExtractSidebarNav(string html)
    {
        var start = html.IndexOf("<nav class=\"dashboard-sidebar\">", StringComparison.Ordinal);
        var end = html.IndexOf("</nav>", start, StringComparison.Ordinal);
        return html[start..end];
    }

    private static string ExtractScriptContaining(string html, string marker)
    {
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        var scriptStart = html.IndexOf("<script>", start, StringComparison.Ordinal);
        var scriptEnd = html.IndexOf("</script>", scriptStart, StringComparison.Ordinal);
        return html[scriptStart..scriptEnd];
    }

#pragma warning disable CC0042 // Four independent test-fixture inputs; a parameter object would just repackage them without benefit.
    private static ByArcGisServiceAggregateRow ServiceRow(string serviceName, string site, DateOnly localDate, int hits) => new()
#pragma warning restore CC0042
    {
        LocalDate = localDate,
        Site = site,
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = hits,
        FailedTimeTakenSecond = 0,
        Hits = hits,
        SuccessfulHits = hits,
        FailedHits = 0,
    };

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
