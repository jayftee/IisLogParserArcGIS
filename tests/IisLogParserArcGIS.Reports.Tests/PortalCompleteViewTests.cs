using System.Text.Json;
using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies the Portal section-wide Complete View (ticket 15) end-to-end against the real, harvested fixture
/// database (ticket 09), per spec.md's "primary seam" testing decision: drive <see cref="RegenerationRun.Run"/>
/// and assert against the files it writes, rather than against internal query/render classes directly.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class PortalCompleteViewTests
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes the All (year-to-date) range fall entirely within real harvested data.
    private static readonly DateOnly _today = new(2026, 9, 10);
    private static readonly DateOnly _yearStart = new(2026, 1, 1);

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public PortalCompleteViewTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Run_WritesTheCompleteViewPage()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "portal", "complete-view.html")));
    }

    [Fact]
    public void Run_SidebarOnTheCompleteViewPage_ListsACompleteViewEntryUnderPortal()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "portal", "complete-view.html"));

        Assert.Contains("portal/complete-view.html\" class=\"active\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_CompleteView_LeavesPagingOff_SoEveryRowIsShown()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "portal", "complete-view.html"));

        Assert.DoesNotContain("page: 'enable'", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_CompleteView_ListsEveryPortalItem_MatchingAnIndependentlyComputedOracle()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "portal", "complete-view.html"));

        var oracleRows = connection.Query<(string PortalItemId, int Hits)>(
            """
            SELECT portal_item_id AS PortalItemId, SUM(hits) AS Hits
            FROM aggregated_by_portal_item
            WHERE local_date BETWEEN @Start AND @End
            GROUP BY portal_item_id
            """,
            new { Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleRows);
        var tableRows = ExtractTableRows(html);

        foreach (var oracleRow in oracleRows)
        {
            var expectedHref = $"https://{settings.PortalBaseUrl}/home/item.html?id={Uri.EscapeDataString(oracleRow.PortalItemId)}";
            var expectedLink = $"<a href=\"{expectedHref}\" target=\"_blank\" rel=\"noopener noreferrer\">{oracleRow.PortalItemId}</a>";
            var matchingRow = FindRow(tableRows, oracleRow.PortalItemId);

            Assert.Equal(oracleRow.Hits, matchingRow[1].GetInt32());
            Assert.Equal(expectedLink, matchingRow[0].GetProperty("f").GetString());
        }
    }

    [Fact]
    public void Run_CompleteView_RendersUnconditionally_WithNoIncludedRoots()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = new ReportsSettings
        {
            LocalTimeZone = "UTC",
            IncludedRoots = [],
            PortalBaseUrl = "portal.example.com",
            OutputDirectory = "Dashboard",
        };

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, "portal", "complete-view.html"));
        var tableRows = ExtractTableRows(html);

        Assert.NotEmpty(tableRows);
    }

    private static JsonElement[] ExtractTableRows(string html)
    {
        const string Marker = "google.visualization.arrayToDataTable(";
        var start = html.IndexOf(Marker, StringComparison.Ordinal) + Marker.Length;
        var end = html.IndexOf(");", start, StringComparison.Ordinal);

        return JsonDocument.Parse(html[start..end]).RootElement.EnumerateArray().ToArray();
    }

    private static JsonElement FindRow(IReadOnlyList<JsonElement> tableRows, string portalItemId)
    {
        foreach (var row in tableRows)
        {
            if (row[0].ValueKind == JsonValueKind.Object && row[0].GetProperty("v").GetString() == portalItemId)
            {
                return row;
            }
        }

        throw new InvalidOperationException($"No Complete View row found for Portal item '{portalItemId}'.");
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
