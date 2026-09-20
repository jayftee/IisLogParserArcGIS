using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Reports.Rendering;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// One per-device Dashboard section (Fieldmaps, ticket 21; Survey123, ticket 22): the source table, the operator's
/// spelling of the section name, and the slugs and element-id prefix its pages use. The Top-50 Leaderboard,
/// Complete View and Detail Page builders are written once over this, so the two sections cannot drift apart.
/// </summary>
public sealed record DeviceSection
{
#pragma warning disable CC0309 // "Fieldmaps" is the operator's spelling of the Esri product name (per ticket 21), not a log field.
    /// <summary>
    /// Gets the Fieldmaps section, over <see cref="DeviceAggregateTable.FieldMaps"/>.
    /// </summary>
    public static DeviceSection Fieldmaps { get; } = new()
    {
        Title = DashboardSidebar.FieldmapsSectionTitle,
        SectionSlug = DashboardSidebar.FieldmapsSectionSlug,
        Table = DeviceAggregateTable.FieldMaps,
    };
#pragma warning restore CC0309

    /// <summary>
    /// Gets the Survey123 section, over <see cref="DeviceAggregateTable.Survey123"/>.
    /// </summary>
    public static DeviceSection Survey123 { get; } = new()
    {
        Title = DashboardSidebar.Survey123SectionTitle,
        SectionSlug = DashboardSidebar.Survey123SectionSlug,
        Table = DeviceAggregateTable.Survey123,
    };

    /// <summary>
    /// Gets every per-device section, in the order the Dashboard builds them and lists them in the sidebar.
    /// </summary>
    public static IReadOnlyList<DeviceSection> All { get; } = [Fieldmaps, Survey123];

    /// <summary>
    /// Gets the section's name as the operator wrote it, for page titles and the sidebar header.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the section's output-relative folder segment, which is also the prefix of every element id it emits.
    /// </summary>
    public required string SectionSlug { get; init; }

    /// <summary>
    /// Gets the by-device aggregate table the section reports on.
    /// </summary>
    public required DeviceAggregateTable Table { get; init; }

    /// <summary>
    /// Gets the output-relative path of the section's Complete View page.
    /// </summary>
    public string CompleteViewHref => DashboardSidebar.DeviceCompleteViewHref(SectionSlug);

    /// <summary>
    /// Builds the output-relative path of the section's Top-50-by-hits Leaderboard page.
    /// </summary>
    /// <param name="range">Which range variant the page shows.</param>
    /// <returns>The page's output-relative path.</returns>
    public string LeaderboardHitsHref(DashboardDateRange range) =>
        DashboardSidebar.DevicePageHref(SectionSlug, DashboardSidebar.DeviceLeaderboardHitsSlug, range);

    /// <summary>
    /// Builds the output-relative path of one device's Detail Page.
    /// </summary>
    /// <param name="deviceId">The casefolded device id.</param>
    /// <returns>The page's output-relative path.</returns>
    public string DetailHref(string deviceId) => DashboardSidebar.DeviceDetailHref(SectionSlug, deviceId);
}
