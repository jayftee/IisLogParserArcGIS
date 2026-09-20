using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Reports.Rendering;
using IisLogParserArcGIS.Reports.Sections;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using static IisLogParserArcGIS.Reports.Tests.TestSupport.DeviceSectionTestSupport;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies every per-device section's sidebar group and Top-50 Leaderboard pages (Fieldmaps, ticket 21;
/// Survey123, ticket 22) end-to-end against the real, harvested fixture database (ticket 09), per spec.md's
/// "primary seam" testing decision: drive <see cref="RegenerationRun.Run"/> and assert against the files it
/// writes. Each behaviour is written once and run for every section in <see cref="DeviceSection.All"/>, so the two
/// sections cannot drift apart.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class DeviceLeaderboardSectionBuilderTests
{
    private const string LabelSeparator = " · ";

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public DeviceLeaderboardSectionBuilderTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_WritesBothLeaderboardPages(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings("arcgis"), CreateTimeProvider(), outputDirectory.Path);

        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, section.SectionSlug, "leaderboard-hits", "all.html")));
        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, section.SectionSlug, "leaderboard-hits", "last-7-days.html")));
    }

    [Fact]
    public void Run_Sidebar_ListsEveryDeviceSectionInOrderBetweenArcGisServerAndLeaderboard_WithNoDetailPageEntry()
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings("arcgis"), CreateTimeProvider(), outputDirectory.Path);

        var nav = ExtractSidebarNav(ReadPage(outputDirectory.Path, DeviceSection.Fieldmaps.LeaderboardHitsHref(DashboardDateRange.All)));
        var arcGisServer = nav.IndexOf(">ArcGIS Server</summary>", StringComparison.Ordinal);
        var leaderboard = nav.IndexOf(">Leaderboard</summary>", StringComparison.Ordinal);
        var sectionStarts = DeviceSection.All.Select(section => nav.IndexOf($">{section.Title}</summary>", StringComparison.Ordinal)).ToArray();

        Assert.True(arcGisServer >= 0, "ArcGIS Server must be in the sidebar.");
        Assert.Equal(sectionStarts.Order(), sectionStarts);
        Assert.True(sectionStarts[0] > arcGisServer && leaderboard > sectionStarts[^1], "The device sections must sit between ArcGIS Server and Leaderboard, in order.");

        for (var index = 0; index < DeviceSection.All.Count; index++)
        {
            var section = DeviceSection.All[index];
            var groupEnd = index + 1 < sectionStarts.Length ? sectionStarts[index + 1] : leaderboard;
            var group = nav[sectionStarts[index]..groupEnd];

            Assert.Contains(">Leaderboard: Hits</summary>", group, StringComparison.Ordinal);
            Assert.Contains(section.LeaderboardHitsHref(DashboardDateRange.All), group, StringComparison.Ordinal);
            Assert.Contains(section.LeaderboardHitsHref(DashboardDateRange.Last7Days), group, StringComparison.Ordinal);
            Assert.Contains(section.CompleteViewHref, group, StringComparison.Ordinal);
            Assert.DoesNotContain($"{section.SectionSlug}/detail/", nav, StringComparison.Ordinal);

            foreach (var other in DeviceSection.All.Where(candidate => candidate.SectionSlug != section.SectionSlug))
            {
                Assert.DoesNotContain($"{other.SectionSlug}/", group, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_RendersUnconditionally_WithAnEmptyIncludedRootAllowList(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.All));

        Assert.NotEmpty(ExtractDataTableRows(html).Skip(1));
        Assert.Contains($">{section.Title}</summary>", html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_LeaderboardAll_MatchesAnIndependentlyComputedOracle(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings("arcgis"), CreateTimeProvider(), outputDirectory.Path);

        AssertLeaderboardMatchesOracle(connection, section, ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.All)), YearStart);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_LeaderboardLast7Days_MatchesAnIndependentlyComputedOracle(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings("arcgis"), CreateTimeProvider(), outputDirectory.Path);

        AssertLeaderboardMatchesOracle(connection, section, ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.Last7Days)), Last7DaysStart);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_LeaderboardLabels_ArePerDevice_UsernameThenTheCompleteUntruncatedDeviceId_OrTheDeviceIdAlone(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DeleteAllDeviceRows(section);

        const string AttributedOne = "227c3d43-ea74-4d05-aaca-1e47ac4bcde9";
        const string AttributedTwo = "11111111-2222-3333-4444-555555555555";
        const string Unattributed = "99999999-8888-7777-6666-555555555555";
        workingCopy.InsertDeviceRows(
            section,
            DeviceRow(AttributedOne, "user00364@somewhere", Today, 30),
            DeviceRow(AttributedTwo, "user00364@somewhere", Today, 20),
            DeviceRow(Unattributed, null, Today, 10));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var rows = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.All))).Skip(1).ToArray();

        Assert.Equal(
            ["user00364@somewhere" + LabelSeparator + AttributedOne, "user00364@somewhere" + LabelSeparator + AttributedTwo, Unattributed],
            rows.Select(row => row[0].GetString()));
        Assert.Equal([30, 20, 10], rows.Select(row => row[1].GetInt32()));
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_LeaderboardWithFewerThan50Devices_RendersOnlyThoseBars_NoPlaceholders(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DeleteAllDeviceRows(section);
        workingCopy.InsertDeviceRows(
            section,
            DeviceRow("device-a", "amy", Today, 3),
            DeviceRow("device-b", null, Today, 2));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var rows = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.All))).Skip(1).ToArray();

        Assert.Equal(2, rows.Length);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_LeaderboardForARangeWithNoRows_RendersTheNoDataCard(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DeleteAllDeviceRows(section);

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        foreach (var range in new[] { DashboardDateRange.All, DashboardDateRange.Last7Days })
        {
            var html = ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(range));

            Assert.Contains("No data for this range.", html, StringComparison.Ordinal);
            Assert.DoesNotContain("arrayToDataTable", html, StringComparison.Ordinal);
        }
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_AgainstADatabaseWithoutTheSectionsTable_StillRendersAnEmptyDataCard_AndTheOtherSectionIsUnaffected(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        var other = OtherSection(section);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DropDeviceTable(section);

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        Assert.Contains("No data for this range.", ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.All)), StringComparison.Ordinal);
        Assert.NotEmpty(ExtractDataTableRows(ReadPage(outputDirectory.Path, other.LeaderboardHitsHref(DashboardDateRange.All))).Skip(1));
    }

    [Fact]
    public void Run_TheDeviceSectionsCoexistOverOneDatabase_EachShowingOnlyItsOwnDevices()
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);

        foreach (var section in DeviceSection.All)
        {
            workingCopy.DeleteAllDeviceRows(section);
            workingCopy.InsertDeviceRows(section, DeviceRow(OwnDeviceId(section), $"{section.SectionSlug}.user", Today, 11));
        }

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        foreach (var section in DeviceSection.All)
        {
            var other = OtherSection(section);
            var leaderboard = ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.All));
            var completeView = ReadPage(outputDirectory.Path, section.CompleteViewHref);
            var detailFiles = Directory.GetFiles(Path.Combine(outputDirectory.Path, section.SectionSlug, "detail"));

            Assert.Contains(OwnDeviceId(section), leaderboard, StringComparison.Ordinal);
            Assert.DoesNotContain(OwnDeviceId(other), leaderboard, StringComparison.Ordinal);
            Assert.Contains(OwnDeviceId(section), completeView, StringComparison.Ordinal);
            Assert.DoesNotContain(OwnDeviceId(other), completeView, StringComparison.Ordinal);
            Assert.Single(detailFiles);
            Assert.True(File.Exists(Path.Combine(outputDirectory.Path, section.DetailHref(OwnDeviceId(section)).Replace('/', Path.DirectorySeparatorChar))));
            Assert.False(File.Exists(Path.Combine(outputDirectory.Path, section.DetailHref(OwnDeviceId(other)).Replace('/', Path.DirectorySeparatorChar))));
        }
    }

    private static string OwnDeviceId(DeviceSection section) => $"{section.SectionSlug}-only";

#pragma warning disable CC0042 // Four independent inputs to one oracle comparison; bundling them would just repackage them.
    private static void AssertLeaderboardMatchesOracle(SqliteConnection connection, DeviceSection section, string html, DateOnly start)
#pragma warning restore CC0042
    {
        var oracleTotals = connection
            .Query<(string DeviceId, int Hits)>(
                $"""
                SELECT device_id, SUM(hits)
                FROM {section.Table.Name}
                WHERE local_date BETWEEN @Start AND @End
                GROUP BY device_id
                ORDER BY 2 DESC
                """,
                new { Start = start, End = Today })
            .ToArray();
        var oracleHitsByDevice = oracleTotals.ToDictionary(row => row.DeviceId, row => row.Hits);

        Assert.NotEmpty(oracleTotals);

        var bars = ExtractDataTableRows(html).Skip(1).ToArray();

        Assert.Equal(Math.Min(50, oracleTotals.Length), bars.Length);
        Assert.Equal(oracleTotals.Take(bars.Length).Select(row => row.Hits), bars.Select(bar => bar[1].GetInt32()));

        foreach (var bar in bars)
        {
            var label = bar[0].GetString()!;
            var separatorIndex = label.LastIndexOf(LabelSeparator, StringComparison.Ordinal);
            var deviceId = separatorIndex < 0 ? label : label[(separatorIndex + LabelSeparator.Length)..];

            Assert.True(oracleHitsByDevice.ContainsKey(deviceId), $"Bar label '{label}' does not end in a known device id.");
            Assert.Equal(oracleHitsByDevice[deviceId], bar[1].GetInt32());

            var oracleUsername = connection.QuerySingleOrDefault<string?>(
                $"""
                SELECT username
                FROM {section.Table.Name}
                WHERE device_id = @DeviceId AND username IS NOT NULL
                ORDER BY local_date, username
                LIMIT 1
                """,
                new { DeviceId = deviceId });
            var expectedLabel = oracleUsername is null ? deviceId : $"{oracleUsername}{LabelSeparator}{deviceId}";

            Assert.Equal(expectedLabel, label);
        }
    }

    private static string ExtractSidebarNav(string html)
    {
        var start = html.IndexOf("<nav class=\"dashboard-sidebar\">", StringComparison.Ordinal);
        var end = html.IndexOf("</nav>", start, StringComparison.Ordinal);
        return html[start..end];
    }
}
