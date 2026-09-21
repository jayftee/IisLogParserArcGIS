using System.Text.Json;
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
/// Verifies the ArcGIS Server section-wide Complete View (ticket 12) end-to-end against the real, harvested
/// fixture database (ticket 09), per spec.md's "primary seam" testing decision: drive
/// <see cref="RegenerationRun.Run"/> and assert against the files it writes, rather than against internal
/// query/render classes directly.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class ArcGisServerCompleteViewTests
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes the All (year-to-date) range fall entirely within real harvested data.
    private static readonly DateOnly _today = new(2026, 9, 10);
    private static readonly DateOnly _yearStart = new(2026, 1, 1);

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public ArcGisServerCompleteViewTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Run_WritesTheCompleteViewPage()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "arcgis-server", "complete-view.html")));
    }

    [Fact]
    public void Run_SidebarOnTheCompleteViewPage_ListsACompleteViewEntryUnderArcGisServer()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "arcgis-server", "complete-view.html"));

        Assert.Contains("arcgis-server/complete-view.html\" class=\"active\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_CompleteView_LeavesPagingOff_SoEveryRowIsShown()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "arcgis-server", "complete-view.html"));

        Assert.DoesNotContain("page: 'enable'", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_CompleteView_ListsEveryServiceAcrossEveryIncludedRoot_MatchingAnIndependentlyComputedOracle()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "arcgis-server", "complete-view.html"));

        var oracleRows = connection.Query<(string Site, string? Folder, string ServiceName, string ServiceType, int Hits)>(
            """
            SELECT site AS Site, folder AS Folder, service_name AS ServiceName, service_type AS ServiceType, SUM(hits) AS Hits
            FROM aggregated_by_arcgis_service
            WHERE site IN @IncludedRoots AND local_date BETWEEN @Start AND @End
            GROUP BY site, folder, service_name, service_type
            """,
            new { IncludedRoots = settings.IncludedRoots, Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleRows);
        var tableRows = ExtractTableRows(html);

        foreach (var oracleRow in oracleRows)
        {
            var detailHref = RelativeLinkHelper.ComputeRootPrefix(DashboardSidebar.ArcGisServerCompleteViewHref)
                + DashboardSidebar.ArcGisServiceDetailHref(oracleRow.Site, oracleRow.Folder, oracleRow.ServiceName, oracleRow.ServiceType);

            var matchingRow = FindRow(tableRows, oracleRow.Site, oracleRow.Folder, oracleRow.ServiceName, oracleRow.ServiceType);

            Assert.Equal(oracleRow.Hits, matchingRow[4].GetInt32());
            Assert.Equal(
                ExpectedLiveLink(settings.ArcGisServerBaseUrl, oracleRow.Site, oracleRow.Folder, oracleRow.ServiceName, oracleRow.ServiceType),
                matchingRow[2].GetProperty("f").GetString());
            Assert.Equal($"<a href=\"{detailHref}\">Details</a>", matchingRow[9].GetString());
        }
    }

    [Fact]
    public void Run_CompleteView_ExcludesTheUnlistedProxyRoot()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "arcgis-server", "complete-view.html"));

        Assert.DoesNotContain("\"proxy\",", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_CompleteView_RepresentsARealisticPerSiteSpread_SomeRootsWithFewServicesAndOneWithMany()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        var edgeCaseDate = _today;
        var manyServiceRows = Enumerable.Range(1, 60)
            .Select(index => LowServiceRow($"busy-service-{index}", "titan", edgeCaseDate))
            .ToArray();
        workingCopy.InsertArcGisServiceRows(manyServiceRows);
        workingCopy.InsertArcGisServiceRows(LowServiceRow("quiet-service", "rhea", edgeCaseDate));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "arcgis-server", "complete-view.html"));
        var rootPrefix = RelativeLinkHelper.ComputeRootPrefix(DashboardSidebar.ArcGisServerCompleteViewHref);
        var baseUrl = CreateSettings().ArcGisServerBaseUrl;
        var tableRows = ExtractTableRows(html);

        for (var index = 1; index <= 60; index++)
        {
            var serviceName = $"busy-service-{index}";
            var detailHref = rootPrefix + DashboardSidebar.ArcGisServiceDetailHref("titan", null, serviceName, "MapServer");
            var matchingRow = FindRow(tableRows, "titan", null, serviceName, "MapServer");

            Assert.Equal(ExpectedLiveLink(baseUrl, "titan", null, serviceName, "MapServer"), matchingRow[2].GetProperty("f").GetString());
            Assert.Equal($"<a href=\"{detailHref}\">Details</a>", matchingRow[9].GetString());
        }

        var quietServiceHref = rootPrefix + DashboardSidebar.ArcGisServiceDetailHref("rhea", null, "quiet-service", "MapServer");
        var quietServiceRow = FindRow(tableRows, "rhea", null, "quiet-service", "MapServer");

        Assert.Equal(ExpectedLiveLink(baseUrl, "rhea", null, "quiet-service", "MapServer"), quietServiceRow[2].GetProperty("f").GetString());
        Assert.Equal($"<a href=\"{quietServiceHref}\">Details</a>", quietServiceRow[9].GetString());
    }

    [Fact]
    public void Run_CompleteView_EscapesAttackerInfluencedFolderAndServiceType_SoTheyCannotInjectMarkup()
    {
        const string FolderMarkup = "<img src=x onerror=alert(1)>";
        const string ServiceTypeMarkup = "<svg/onload=alert(1)>Server";
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertArcGisServiceRows(
            LowServiceRow("xss-service", "titan", _today) with { Folder = FolderMarkup, ServiceType = ServiceTypeMarkup });
        var settings = CreateSettings();

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "arcgis-server", "complete-view.html"));
        var row = ExtractTableRows(html).Skip(1).Single(candidate => candidate[2].GetProperty("v").GetString() == "xss-service");

        Assert.Equal("titan", DeviceSectionTestSupport.CellText(row[0]));
        Assert.Equal(FolderMarkup, DeviceSectionTestSupport.CellText(row[1]));
        Assert.Equal(ServiceTypeMarkup, DeviceSectionTestSupport.CellText(row[3]));
        Assert.All([row[0], row[1], row[3]], cell => Assert.DoesNotContain("<", cell.GetProperty("f").GetString(), StringComparison.Ordinal));
    }

#pragma warning disable CC0042 // Five independent identity components an expected live-endpoint link is built from; a parameter object would just repackage them without benefit.
    private static string ExpectedLiveLink(string baseUrl, string site, string? folder, string serviceName, string serviceType)
#pragma warning restore CC0042
    {
        var folderSegment = string.IsNullOrEmpty(folder) ? string.Empty : $"{Uri.EscapeDataString(folder)}/";
        var href = $"https://{baseUrl}/{Uri.EscapeDataString(site)}/rest/services/{folderSegment}{Uri.EscapeDataString(serviceName)}/{Uri.EscapeDataString(serviceType)}";

        return $"<a href=\"{href}\" target=\"_blank\" rel=\"noopener noreferrer\">{serviceName}</a>";
    }

    private static JsonElement[] ExtractTableRows(string html)
    {
        const string Marker = "google.visualization.arrayToDataTable(";
        var start = html.IndexOf(Marker, StringComparison.Ordinal) + Marker.Length;
        var end = html.IndexOf(");", start, StringComparison.Ordinal);

        return JsonDocument.Parse(html[start..end]).RootElement.EnumerateArray().ToArray();
    }

#pragma warning disable CC0042 // Four independent identity components plus the table to search; a parameter object would just repackage them without benefit.
    private static JsonElement FindRow(IReadOnlyList<JsonElement> tableRows, string site, string? folder, string serviceName, string serviceType)
#pragma warning restore CC0042
    {
        foreach (var row in tableRows)
        {
            if (DeviceSectionTestSupport.CellText(row[0]) == site
                && DeviceSectionTestSupport.CellText(row[1]) == (folder ?? string.Empty)
                && row[2].GetProperty("v").GetString() == serviceName
                && DeviceSectionTestSupport.CellText(row[3]) == serviceType)
            {
                return row;
            }
        }

        throw new InvalidOperationException($"No Complete View row found for {site}/{folder}/{serviceName}/{serviceType}.");
    }

    private static ByArcGisServiceAggregateRow LowServiceRow(string serviceName, string site, DateOnly localDate) => new()
    {
        LocalDate = localDate,
        Site = site,
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = 1.0,
        FailedTimeTakenSecond = 0,
        Hits = 1,
        SuccessfulHits = 1,
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
            ArcGisServerBaseUrl = "gis.example.com",
            OutputDirectory = "Dashboard",
        };
    }
}
