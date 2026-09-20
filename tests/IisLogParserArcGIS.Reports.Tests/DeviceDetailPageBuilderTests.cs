using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using static IisLogParserArcGIS.Reports.Tests.TestSupport.DeviceSectionTestSupport;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies every per-device section's Detail Pages (Fieldmaps, ticket 21; Survey123, ticket 22) end-to-end
/// against the real, harvested fixture database (ticket 09): drive <see cref="RegenerationRun.Run"/> and assert
/// against the files it writes. Each behaviour is written once and run for every section in
/// <see cref="Sections.DeviceSection.All"/>.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class DeviceDetailPageBuilderTests
{
    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public DeviceDetailPageBuilderTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_WritesExactlyOneDetailPagePerDeviceTheCompleteViewLists_NoSeparateRangePages(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var deviceIds = ExtractDataTableRows(ReadPage(outputDirectory.Path, section.CompleteViewHref))
            .Skip(1)
            .Select(row => CellText(row[1]))
            .ToArray();
        var expectedFiles = deviceIds
            .Select(deviceId => Path.GetFullPath(Path.Combine(outputDirectory.Path, section.DetailHref(deviceId).Replace('/', Path.DirectorySeparatorChar))))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var actualFiles = Directory
            .GetFiles(Path.Combine(outputDirectory.Path, section.SectionSlug, "detail"), "*", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(deviceIds);
        Assert.Equal(expectedFiles, actualFiles);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_DetailPage_HasNoSidebarEntry_AndOffersABackLinkToTheCompleteView(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertDeviceRows(section, DeviceRow("back-device", "amy", Today, 3));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = ReadPage(outputDirectory.Path, section.DetailHref("back-device"));
        var nav = html[html.IndexOf("<nav class=\"dashboard-sidebar\">", StringComparison.Ordinal)..html.IndexOf("</nav>", StringComparison.Ordinal)];

        Assert.DoesNotContain($"{section.SectionSlug}/detail/", nav, StringComparison.Ordinal);
        Assert.Contains($"<a href=\"../../{section.CompleteViewHref}\" class=\"range-toggle-button detail-back-link\">Back</a>", html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_DetailPage_EmbedsBothRangesForAnInPageToggle(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertDeviceRows(section, DeviceRow("toggle-device", "amy", Today, 3));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = ReadPage(outputDirectory.Path, section.DetailHref("toggle-device"));

        Assert.Contains("data-range=\"all\"", html, StringComparison.Ordinal);
        Assert.Contains("data-range=\"last-7-days\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"detail-range-template\"", html, StringComparison.Ordinal);
        Assert.Contains("google.visualization.AnnotatedTimeLine", html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_DetailPage_ShowsTheUsernameAndDeviceId_OrUnattributed(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertDeviceRows(
            section,
            DeviceRow("known-device", "amy", Today, 3),
            DeviceRow("unknown-device", null, Today, 3));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var known = ReadPage(outputDirectory.Path, section.DetailHref("known-device"));
        var unknown = ReadPage(outputDirectory.Path, section.DetailHref("unknown-device"));

        Assert.Contains(section.Title, known, StringComparison.Ordinal);
        Assert.Contains("amy", known, StringComparison.Ordinal);
        Assert.Contains("known-device", known, StringComparison.Ordinal);
        Assert.Contains("Unattributed", unknown, StringComparison.Ordinal);
        Assert.Contains("unknown-device", unknown, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_DetailPage_ChartsSpanOnlyTheDevicesRealDates_NoZeroPaddingBetweenSparseDates(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        var earlyDate = YearStart.AddDays(10);
        var lateDate = Today.AddDays(-30);
        var middleDate = YearStart.AddDays(100);
        workingCopy.InsertDeviceRows(
            section,
            DeviceRow("sparse-device", "amy", earlyDate, 2),
            DeviceRow("sparse-device", "amy", lateDate, 5));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = ReadPage(outputDirectory.Path, section.DetailHref("sparse-device"));
        var chartScript = ExtractScriptContaining(html, $"{section.SectionSlug}-device-detail-hits-chart-all");

        Assert.Contains(DateLiteral(earlyDate), chartScript, StringComparison.Ordinal);
        Assert.Contains(DateLiteral(lateDate), chartScript, StringComparison.Ordinal);
        Assert.DoesNotContain(DateLiteral(middleDate), chartScript, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(chartScript, "new Date("));
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_DetailPage_PlotsPerDayHitsNotCumulative(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertDeviceRows(
            section,
            DeviceRow("daily-device", "amy", Today.AddDays(-2), 4),
            DeviceRow("daily-device", "amy", Today.AddDays(-1), 1));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = ReadPage(outputDirectory.Path, section.DetailHref("daily-device"));
        var chartScript = ExtractScriptContaining(html, $"{section.SectionSlug}-device-detail-hits-chart-all");
        var yesterday = Today.AddDays(-1);

        Assert.Contains($"[new Date({yesterday.Year}, {yesterday.Month - 1}, {yesterday.Day}), 1]", chartScript, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_DetailPage_LastSevenDaysExcludesOlderDates(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        var oldDate = Today.AddDays(-30);
        workingCopy.InsertDeviceRows(
            section,
            DeviceRow("window-device", "amy", oldDate, 9),
            DeviceRow("window-device", "amy", Today, 1));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var html = ReadPage(outputDirectory.Path, section.DetailHref("window-device"));
        var last7Script = ExtractScriptContaining(html, $"{section.SectionSlug}-device-detail-hits-chart-last-7-days");

        Assert.Equal(1, CountOccurrences(last7Script, "new Date("));
        Assert.DoesNotContain(DateLiteral(oldDate), last7Script, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_DetailPage_ForADeviceIdSharedWithTheOtherSection_ChartsOnlyItsOwnSectionsHits(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        var other = OtherSection(section);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.InsertDeviceRows(other, DeviceRow("shared-id", "amy", Today.AddDays(-5), 99));
        workingCopy.InsertDeviceRows(section, DeviceRow("shared-id", "amy", Today.AddDays(-1), 4));

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        var chartScript = ExtractScriptContaining(ReadPage(outputDirectory.Path, section.DetailHref("shared-id")), $"{section.SectionSlug}-device-detail-hits-chart-all");
        var yesterday = Today.AddDays(-1);

        Assert.Equal(1, CountOccurrences(chartScript, "new Date("));
        Assert.Contains($"[new Date({yesterday.Year}, {yesterday.Month - 1}, {yesterday.Day}), 4]", chartScript, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void Run_AgainstADatabaseWithoutTheSectionsTable_WritesNoDetailPagesForIt(string sectionSlug)
    {
        var section = SectionFor(sectionSlug);
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DropDeviceTable(section);

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path);

        Assert.False(Directory.Exists(Path.Combine(outputDirectory.Path, section.SectionSlug, "detail")));
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

    private static string ExtractScriptContaining(string html, string marker)
    {
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        var scriptStart = html.IndexOf("<script>", start, StringComparison.Ordinal);
        var scriptEnd = html.IndexOf("</script>", scriptStart, StringComparison.Ordinal);
        return html[scriptStart..scriptEnd];
    }
}
