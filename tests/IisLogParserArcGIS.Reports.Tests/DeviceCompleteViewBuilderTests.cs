using System.Globalization;
using System.Net;
using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Reports.Rendering;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using static IisLogParserArcGIS.Reports.Tests.TestSupport.DeviceSectionTestSupport;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies every per-device section's Complete View (Fieldmaps, ticket 21; Survey123, ticket 22) end-to-end
/// against the real, harvested fixture database (ticket 09): drive <see cref="RegenerationRun.Run"/> and assert
/// against the file it writes. Each behaviour is written once and run for every section in
/// <see cref="Sections.DeviceSection.All"/>.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class DeviceCompleteViewBuilderTests
{
    private static readonly string[] _expectedHeaders =
    [
        "Username", "Device ID", "Hits", "Total Time Taken (s)", "Average Time Taken (s)", "Last Seen", "Detail Page",
    ];

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public DeviceCompleteViewBuilderTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_CompleteView_HasTheSevenColumns_AndPagingOff_SoEveryRowIsShown(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = ReadPage(outputDirectory.Path, section.CompleteViewHref);
        var header = ExtractDataTableRows(html)[0].EnumerateArray().Select(cell => cell.GetString()).ToArray();

        Assert.Equal(_expectedHeaders, header);
        Assert.DoesNotContain("page: 'enable'", html, StringComparison.Ordinal);
        Assert.Contains($"{section.SectionSlug}-complete-view-table", html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_CompleteView_ListsEveryDevice_AttributedOrNot_MatchingAnIndependentlyComputedOracle(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var rows = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.CompleteViewHref)).Skip(1).ToArray();
        var oracle = connection
            .Query<(string DeviceId, int Hits, double TotalTime, string LastSeen)>(
                $"""
                SELECT device_id, SUM(hits), SUM(time_taken_second), MAX(local_date)
                FROM {section.Table.Name}
                WHERE local_date BETWEEN @Start AND @End
                GROUP BY device_id
                """,
                new { Start = YearStart, End = Today })
            .ToArray();

        Assert.NotEmpty(oracle);
        Assert.Equal(oracle.Length, rows.Length);
        Assert.Contains(rows, row => string.IsNullOrEmpty(CellText(row[0])));
        Assert.Contains(rows, row => CellText(row[0]).Length > 0);

        var rowsByDevice = rows.ToDictionary(row => CellText(row[1]));

        foreach (var expected in oracle)
        {
            var row = rowsByDevice[expected.DeviceId];
            var oracleUsername = connection.QuerySingleOrDefault<string?>(
                $"""
                SELECT username
                FROM {section.Table.Name}
                WHERE device_id = @DeviceId AND username IS NOT NULL
                ORDER BY local_date, username
                LIMIT 1
                """,
                new { DeviceId = expected.DeviceId });

            Assert.Equal(oracleUsername ?? string.Empty, CellText(row[0]));
            Assert.Equal(expected.Hits, row[2].GetInt32());
            Assert.Equal(Math.Round(expected.TotalTime, 3), row[3].GetDouble(), precision: 6);
            Assert.Equal(Math.Round(expected.TotalTime / expected.Hits, 3), row[4].GetDouble(), precision: 6);
            Assert.Equal(expected.LastSeen, row[5].GetString());
        }
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_CompleteView_LastSeen_IsIsoTextThatSortsChronologically(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var lastSeen = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.CompleteViewHref)).Skip(1).Select(row => row[5].GetString()!).ToArray();

        Assert.All(lastSeen, value => Assert.True(DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)));
        Assert.Equal(
            lastSeen.Order(StringComparer.Ordinal).Select(value => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture)),
            lastSeen.Select(value => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture)).Order());
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_CompleteView_LinksEveryRowToItsDetailPage(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var rows = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.CompleteViewHref)).Skip(1).ToArray();

        Assert.NotEmpty(rows);
        Assert.All(
            rows,
            row =>
            {
                var expectedHref = "../" + section.DetailHref(CellText(row[1]));

                Assert.Contains($"href=\"{WebUtility.HtmlEncode(expectedHref)}\"", row[6].GetString(), StringComparison.Ordinal);
            });
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_CompleteView_ADeviceWithTwoUsernames_ShowsTheEarliestDateThenAlphabeticalOne_SameAsTheLeaderboard(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DeleteAllDeviceRows(section);
        workingCopy.InsertDeviceRows(
            section,
            DeviceRow("lent-device", "zed", Today.AddDays(-20), 5),
            DeviceRow("lent-device", "amy", Today.AddDays(-3), 50),
            DeviceRow("tie-device", "bob", Today.AddDays(-3), 4),
            DeviceRow("tie-device", "amy", Today.AddDays(-3), 4));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var completeView = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.CompleteViewHref)).Skip(1).ToDictionary(row => CellText(row[1]));
        var leaderboardAll = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.All))).Skip(1).Select(row => row[0].GetString()).ToArray();
        var leaderboardLast7 = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.Last7Days))).Skip(1).Select(row => row[0].GetString()).ToArray();

        Assert.Equal("zed", CellText(completeView["lent-device"][0]));
        Assert.Equal("amy", CellText(completeView["tie-device"][0]));
        Assert.Contains("zed · lent-device", leaderboardAll);
        Assert.Contains("zed · lent-device", leaderboardLast7);
        Assert.Contains("amy · tie-device", leaderboardAll);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_CompleteView_EscapesAnAttackerInfluencedUsername_SoItCannotInjectMarkup(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertDeviceRows(section, DeviceRow("xss-device", "<img src=x onerror=alert(1)>", Today, 1));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var row = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.CompleteViewHref)).Skip(1).Single(candidate => CellText(candidate[1]) == "xss-device");

        Assert.DoesNotContain("<img", row[0].GetProperty("f").GetString(), StringComparison.Ordinal);
        Assert.Equal("<img src=x onerror=alert(1)>", row[0].GetProperty("v").GetString());
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_CompleteView_WithNoRowsInTheTable_RendersTheNoDataCard(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DeleteAllDeviceRows(section);

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = ReadPage(outputDirectory.Path, section.CompleteViewHref);

        Assert.Contains("No data for this range.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("arrayToDataTable", html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_CompleteView_AgainstADatabaseWithoutTheSectionsTable_StillRendersTheNoDataCard(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DropDeviceTable(section);

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        Assert.Contains("No data for this range.", ReadPage(outputDirectory.Path, section.CompleteViewHref), StringComparison.Ordinal);
    }
}
