using System.Net;
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
/// Verifies the Portal item Detail Page (ticket 16) end-to-end against the real, harvested fixture database
/// (ticket 09), per spec.md's "primary seam" testing decision: drive <see cref="RegenerationRun.Run"/> and assert
/// against the files it writes, rather than against internal query/render classes directly.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class PortalItemDetailPageTests
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes the All (year-to-date) range fall entirely within real harvested data.
    private static readonly DateOnly _today = new(2026, 9, 10);
    private static readonly DateOnly _yearStart = new(2026, 1, 1);

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public PortalItemDetailPageTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Run_WritesADetailPageForEveryPortalItemTheCompleteViewLists()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var oracleItemIds = connection.Query<string>(
            """
            SELECT portal_item_id
            FROM aggregated_by_portal_item
            WHERE local_date BETWEEN @Start AND @End
            GROUP BY portal_item_id
            """,
            new { Start = _yearStart, End = _today }).ToArray();

        Assert.NotEmpty(oracleItemIds);

        foreach (var portalItemId in oracleItemIds)
        {
            var href = DashboardSidebar.PortalItemDetailHref(portalItemId);
            var fullPath = Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(fullPath), $"Expected a Detail Page at '{href}' for Portal item '{portalItemId}'.");
        }
    }

    [Fact]
    public void Run_DetailPage_HasNoSidebarEntry_ReachableOnlyFromTheCompleteView()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var oracleItemId = connection.QuerySingle<string>(
            """
            SELECT portal_item_id
            FROM aggregated_by_portal_item
            WHERE local_date BETWEEN @Start AND @End
            GROUP BY portal_item_id
            LIMIT 1
            """,
            new { Start = _yearStart, End = _today });

        var href = DashboardSidebar.PortalItemDetailHref(oracleItemId);
        var completeViewHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, "portal", "complete-view.html"));
        var navHtml = ExtractSidebarNav(completeViewHtml);

        Assert.DoesNotContain(href, navHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_DetailPage_IsLinkedFromTheCompleteView()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var oracleItemId = connection.QuerySingle<string>(
            """
            SELECT portal_item_id
            FROM aggregated_by_portal_item
            WHERE local_date BETWEEN @Start AND @End
            GROUP BY portal_item_id
            LIMIT 1
            """,
            new { Start = _yearStart, End = _today });

        var completeViewHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, "portal", "complete-view.html"));
        var detailCell = FindRow(ExtractTableRows(completeViewHtml), oracleItemId)[6].GetString();
        var expectedHref = "../" + DashboardSidebar.PortalItemDetailHref(oracleItemId);

        Assert.Contains($"href=\"{WebUtility.HtmlEncode(expectedHref)}\"", detailCell, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_DetailPage_HasNoLivePortalLink_OnlyItsCompleteViewRowLinksToPortal_AndOffersABackLink()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = CreateSettings();

        RegenerationRun.Run(connection, settings, CreateTimeProvider(), outputDirectory.Path);

        var oracleItemId = connection.QuerySingle<string>(
            """
            SELECT portal_item_id
            FROM aggregated_by_portal_item
            WHERE local_date BETWEEN @Start AND @End
            GROUP BY portal_item_id
            LIMIT 1
            """,
            new { Start = _yearStart, End = _today });

        var href = DashboardSidebar.PortalItemDetailHref(oracleItemId);
        var detailHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar)));
        var completeViewHtml = File.ReadAllText(Path.Combine(outputDirectory.Path, "portal", "complete-view.html"));

        var expectedLiveLink = $"https://{settings.PortalBaseUrl}/home/item.html?id={Uri.EscapeDataString(oracleItemId)}";

        Assert.DoesNotContain("/home/item.html", detailHtml, StringComparison.Ordinal);
        Assert.Contains(expectedLiveLink, completeViewHtml, StringComparison.Ordinal);
        Assert.Contains("<a href=\"../../portal/complete-view.html\" class=\"range-toggle-button detail-back-link\">Back</a>", detailHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_DetailPage_EmbedsBothRangesForAnInPageToggle_NoSeparatePages()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertPortalItemRows(ItemRow("toggle-item", _today, hits: 3));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var href = DashboardSidebar.PortalItemDetailHref("toggle-item");
        var fullPath = Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar));
        var html = File.ReadAllText(fullPath);

        Assert.Contains("data-range=\"all\"", html, StringComparison.Ordinal);
        Assert.Contains("data-range=\"last-7-days\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"detail-range-template\"", html, StringComparison.Ordinal);
        Assert.Contains("range-toggle-button", html, StringComparison.Ordinal);

        var itemDirectory = Path.GetDirectoryName(fullPath)!;
        Assert.Equal(new[] { fullPath }, Directory.GetFiles(itemDirectory, "toggle-item*.html"));
    }

    [Fact]
    public void Run_PortalItemIdWithFilesystemIllegalCharacters_WritesASanitizedDetailPageInstead()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertPortalItemRows(ItemRow("weird*item?", _today, hits: 3));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var href = DashboardSidebar.PortalItemDetailHref("weird*item?");
        var fullPath = Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar));

        Assert.DoesNotContain('*', href);
        Assert.DoesNotContain('?', href);
        Assert.True(File.Exists(fullPath), $"Expected a sanitized Detail Page at '{href}'.");
    }

    [Fact]
    public void Run_DetailPage_ChartsSpanOnlyTheItemsRealDateRange_NoZeroPaddingBetweenSparseDates()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        var earlyDate = _yearStart.AddDays(10);
        var lateDate = _today.AddDays(-10);
        var middleDate = _yearStart.AddDays(150);
        workingCopy.InsertPortalItemRows(
            ItemRow("sparse-item", earlyDate, hits: 2),
            ItemRow("sparse-item", lateDate, hits: 5));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var href = DashboardSidebar.PortalItemDetailHref("sparse-item");
        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, href.Replace('/', Path.DirectorySeparatorChar)));
        var chartScript = ExtractScriptContaining(html, "portal-item-detail-successful-requests-chart-all");

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

    private static ByPortalItemAggregateRow ItemRow(string portalItemId, DateOnly localDate, int hits) => new()
    {
        LocalDate = localDate,
        PortalItemId = portalItemId,
        TimeTakenSecond = hits,
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
