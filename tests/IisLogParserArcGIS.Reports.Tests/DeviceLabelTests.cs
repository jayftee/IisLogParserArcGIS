using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Reports.Rendering;
using IisLogParserArcGIS.Reports.Sections;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using static IisLogParserArcGIS.Reports.Tests.TestSupport.DeviceSectionTestSupport;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies the bar label <c>DeviceLabel</c> gives a device on a per-device section's Top-50 Leaderboard:
/// <c>username · deviceId</c> when attributed, the complete device id alone when not. <c>DeviceLabel</c> is
/// internal, so the rule is driven through its only caller, <see cref="DeviceLeaderboardSectionBuilder"/>, called
/// directly against a working copy of the harvested database with just the rows under test. (Its argument guard
/// for a <see langword="null"/> device id is unreachable through the builder, since a stored device id is never
/// null.) The fixture collection is used only to keep every test that opens pooled SQLite connections serial.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class DeviceLabelTests
{
    private const string LongDeviceId = "227c3d43-ea74-4d05-aaca-1e47ac4bcde9";

    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public DeviceLabelTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void AnAttributedDevice_IsLabelledUsernameThenTheCompleteDeviceId(string sectionSlug)
    {
        var labels = BuildLeaderboardLabels(SectionFor(sectionSlug), DeviceRow(LongDeviceId, "user00364@somewhere", Today, 5));

        Assert.Equal([$"user00364@somewhere · {LongDeviceId}"], labels);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void AnUnattributedDevice_IsLabelledByItsCompleteDeviceIdAlone(string sectionSlug)
    {
        var labels = BuildLeaderboardLabels(SectionFor(sectionSlug), DeviceRow(LongDeviceId, null, Today, 5));

        Assert.Equal([LongDeviceId], labels);
    }

    [Theory]
    [MemberData(nameof(SectionSlugs), MemberType = typeof(DeviceSectionTestSupport))]
    public void TwoDevicesOfOneUser_GetDistinctLabels_BecauseTheDeviceIdIsAlwaysIncluded(string sectionSlug)
    {
        var labels = BuildLeaderboardLabels(
            SectionFor(sectionSlug),
            DeviceRow("device-one", "amy", Today, 9),
            DeviceRow("device-two", "amy", Today, 4));

        Assert.Equal(["amy · device-one", "amy · device-two"], labels);
    }

    private string[] BuildLeaderboardLabels(DeviceSection section, params DeviceTestRow[] rows)
    {
        using var workingCopy = new WorkingDatabaseCopy(_fixture.DatabasePath);
        workingCopy.DeleteAllDeviceRows(section);
        workingCopy.InsertDeviceRows(section, rows);

        using var outputDirectory = new TempDirectoryPath();
        using var connection = workingCopy.OpenConnection();

        DeviceLeaderboardSectionBuilder.Build(
            section,
            connection,
            CreateSettings(),
            new RegenerationRunBoundaries(Today, YearStart, Last7DaysStart),
            new DateTimeOffset(Today, TimeOnly.MinValue, TimeSpan.Zero),
            outputDirectory.Path);

        return ExtractDataTableRows(ReadPage(outputDirectory.Path, section.LeaderboardHitsHref(DashboardDateRange.All)))
            .Skip(1)
            .Select(row => row[0].GetString()!)
            .ToArray();
    }
}
