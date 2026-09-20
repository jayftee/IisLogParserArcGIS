using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Reports.Rendering;
using IisLogParserArcGIS.Reports.Sections;

namespace IisLogParserArcGIS.Reports.Tests;

public sealed class DeviceSectionTests
{
    [Fact]
    public void All_IsExactlyFieldmapsThenSurvey123()
    {
        Assert.Equal([DeviceSection.Fieldmaps, DeviceSection.Survey123], DeviceSection.All);
    }

    [Theory]
    [InlineData("Fieldmaps", "fieldmaps", "aggregated_by_field_maps_device")]
    [InlineData("Survey123", "survey123", "aggregated_by_survey123_device")]
    public void EachSection_HasTheOperatorsSpelling_ItsOwnSlug_AndItsOwnTable(string title, string slug, string tableName)
    {
        var section = DeviceSection.All.Single(candidate => candidate.Title == title);

        Assert.Equal(slug, section.SectionSlug);
        Assert.Equal(tableName, section.Table.Name);
    }

    [Fact]
    public void EachSection_ReadsItsOwnDistinctTable()
    {
        Assert.Same(DeviceAggregateTable.FieldMaps, DeviceSection.Fieldmaps.Table);
        Assert.Same(DeviceAggregateTable.Survey123, DeviceSection.Survey123.Table);
    }

    [Theory]
    [InlineData("fieldmaps", "fieldmaps/leaderboard-hits/all.html", "fieldmaps/leaderboard-hits/last-7-days.html")]
    [InlineData("survey123", "survey123/leaderboard-hits/all.html", "survey123/leaderboard-hits/last-7-days.html")]
    public void LeaderboardHitsHref_BuildsTheDocumentedPagePathForEachRange(string slug, string allHref, string last7DaysHref)
    {
        var section = DeviceSection.All.Single(candidate => candidate.SectionSlug == slug);

        Assert.Equal(allHref, section.LeaderboardHitsHref(DashboardDateRange.All));
        Assert.Equal(last7DaysHref, section.LeaderboardHitsHref(DashboardDateRange.Last7Days));
    }

    [Theory]
    [InlineData("fieldmaps", "fieldmaps/complete-view.html")]
    [InlineData("survey123", "survey123/complete-view.html")]
    public void CompleteViewHref_BuildsTheDocumentedPagePath(string slug, string expectedHref)
    {
        var section = DeviceSection.All.Single(candidate => candidate.SectionSlug == slug);

        Assert.Equal(expectedHref, section.CompleteViewHref);
    }

    [Theory]
    [InlineData("fieldmaps", "227c3d43-ea74-4d05-aaca-1e47ac4bcde9", "fieldmaps/detail/227c3d43-ea74-4d05-aaca-1e47ac4bcde9.html")]
    [InlineData("survey123", "4fd34afef4f84614bf56df048700f256", "survey123/detail/4fd34afef4f84614bf56df048700f256.html")]
    public void DetailHref_PutsTheDeviceIdUnderTheSectionsDetailFolder(string slug, string deviceId, string expectedHref)
    {
        var section = DeviceSection.All.Single(candidate => candidate.SectionSlug == slug);

        Assert.Equal(expectedHref, section.DetailHref(deviceId));
    }

    [Theory]
    [InlineData("fieldmaps")]
    [InlineData("survey123")]
    public void DetailHref_SanitizesTheDeviceIdAsAPathSegment(string slug)
    {
        var section = DeviceSection.All.Single(candidate => candidate.SectionSlug == slug);
        const string UnsafeDeviceId = "we*ird?id";

        var href = section.DetailHref(UnsafeDeviceId);

        Assert.Equal($"{slug}/detail/{DetailPagePathSegment.Sanitize(UnsafeDeviceId)}.html", href);
        Assert.DoesNotContain("*", href, StringComparison.Ordinal);
        Assert.DoesNotContain("?", href, StringComparison.Ordinal);
    }
}
