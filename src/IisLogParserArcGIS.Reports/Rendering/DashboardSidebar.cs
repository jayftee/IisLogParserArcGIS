namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Builds the Dashboard's persistent sidebar tree, per ticket 01's site map: Summary, Portal, ArcGIS Server,
/// Fieldmaps, Survey123, and Leaderboard as top-level entries, with no separate "Home" entry - the sidebar brand and the
/// site's <c>index.html</c> both resolve to Summary → Successful Requests → All instead. Leaderboard (ticket 17)
/// expands into the four flat, standalone Top-500-by-hits Leaderboard pages: forwarded-for IP, referer, URI, and
/// user agent. Portal (tickets 14, 15), Fieldmaps (ticket 21) and Survey123 (ticket 22) render unconditionally, regardless of the
/// Included Root allow-list. ArcGIS Server (ticket 11) renders one sub-group per Included Root. Every group in
/// the tree - top-level sections down to each metric's own range picker - collapses (ticket 19); see
/// <see cref="SidebarNode.Children"/>.
/// </summary>
public static class DashboardSidebar
{
    /// <summary>
    /// The output-relative path of the Dashboard's default landing page (Summary → Successful Requests → All),
    /// reused by <c>index.html</c>, the sidebar brand link, and the sidebar's own "All" entry.
    /// </summary>
    public const string LandingPageHref = "summary/successful-requests/all.html";

    /// <summary>
    /// The output-relative path of Summary's Last 7 Days page.
    /// </summary>
    public const string SummaryLast7DaysHref = "summary/successful-requests/last-7-days.html";

    /// <summary>
    /// The output-relative path of Summary's Complete View page.
    /// </summary>
    public const string SummaryCompleteViewHref = "summary/complete-view.html";

    /// <summary>
    /// The Portal section's output-relative folder segment, under which every Portal page lands.
    /// </summary>
    public const string PortalSectionSlug = "portal";

    /// <summary>
    /// The metric-folder slug for Portal's successful-requests evolution chart.
    /// </summary>
    public const string PortalSuccessfulRequestsSlug = "successful-requests";

    /// <summary>
    /// The metric-folder slug for Portal's failed-requests evolution chart.
    /// </summary>
    public const string PortalFailedRequestsSlug = "failed-requests";

    /// <summary>
    /// The metric-folder slug for Portal's Top-50-by-successful-hits Leaderboard.
    /// </summary>
    public const string PortalLeaderboardSuccessfulHitsSlug = "leaderboard-successful-hits";

    /// <summary>
    /// The metric-folder slug for Portal's Top-50-by-failed-hits Leaderboard.
    /// </summary>
    public const string PortalLeaderboardFailedHitsSlug = "leaderboard-failed-hits";

    /// <summary>
    /// The output-relative path of the Portal section's section-wide Complete View page.
    /// </summary>
    public const string PortalCompleteViewHref = $"{PortalSectionSlug}/complete-view.html";

    /// <summary>
    /// The ArcGIS Server section's output-relative folder segment, under which every per-site page lands.
    /// </summary>
    public const string ArcGisServerSectionSlug = "arcgis-server";

    /// <summary>
    /// The metric-folder slug for a site's successful-requests evolution chart.
    /// </summary>
    public const string ArcGisServerSuccessfulRequestsSlug = "successful-requests";

    /// <summary>
    /// The metric-folder slug for a site's failed-requests evolution chart.
    /// </summary>
    public const string ArcGisServerFailedRequestsSlug = "failed-requests";

    /// <summary>
    /// The metric-folder slug for a site's average-processing-time-for-successful-requests evolution chart.
    /// </summary>
    public const string ArcGisServerAverageTimeSuccessfulSlug = "average-time-successful";

    /// <summary>
    /// The metric-folder slug for a site's average-processing-time-for-failed-requests evolution chart.
    /// </summary>
    public const string ArcGisServerAverageTimeFailedSlug = "average-time-failed";

    /// <summary>
    /// The metric-folder slug for a site's Top-50-by-successful-hits Leaderboard.
    /// </summary>
    public const string ArcGisServerLeaderboardSuccessfulHitsSlug = "leaderboard-successful-hits";

    /// <summary>
    /// The metric-folder slug for a site's Top-50-by-failed-hits Leaderboard.
    /// </summary>
    public const string ArcGisServerLeaderboardFailedHitsSlug = "leaderboard-failed-hits";

    /// <summary>
    /// The metric-folder slug for a site's Top-50-by-average-successful-time Leaderboard.
    /// </summary>
    public const string ArcGisServerLeaderboardAverageTimeSuccessfulSlug = "leaderboard-average-time-successful";

    /// <summary>
    /// The metric-folder slug for a site's Top-50-by-average-failed-time Leaderboard.
    /// </summary>
    public const string ArcGisServerLeaderboardAverageTimeFailedSlug = "leaderboard-average-time-failed";

    /// <summary>
    /// The output-relative path of the ArcGIS Server section's section-wide Complete View page.
    /// </summary>
    public const string ArcGisServerCompleteViewHref = $"{ArcGisServerSectionSlug}/complete-view.html";

#pragma warning disable CC0309 // "Fieldmaps" is the operator's spelling of the Esri product name (per ticket 21), not a log field.
    /// <summary>
    /// The Fieldmaps section's output-relative folder segment, under which every Fieldmaps page lands. Spelled as
    /// the operator wrote it, rather than the product's "Field Maps".
    /// </summary>
    public const string FieldmapsSectionSlug = "fieldmaps";

    /// <summary>
    /// The Fieldmaps section's name as the operator wrote it, for the sidebar header and every page title.
    /// </summary>
    public const string FieldmapsSectionTitle = "Fieldmaps";
#pragma warning restore CC0309

    /// <summary>
    /// The Survey123 section's output-relative folder segment, under which every Survey123 page lands.
    /// </summary>
    public const string Survey123SectionSlug = "survey123";

    /// <summary>
    /// The Survey123 section's name as the operator wrote it, for the sidebar header and every page title.
    /// </summary>
    public const string Survey123SectionTitle = "Survey123";

    /// <summary>
    /// The metric-folder slug for a per-device section's (Fieldmaps or Survey123) Top-50-by-hits Leaderboard.
    /// </summary>
    public const string DeviceLeaderboardHitsSlug = "leaderboard-hits";

    /// <summary>
    /// The Leaderboard section's output-relative folder segment, under which every flat Leaderboard page lands.
    /// </summary>
    public const string LeaderboardSectionSlug = "leaderboard";

    /// <summary>
    /// The dimension-folder slug for the forwarded-for-IP flat Leaderboard.
    /// </summary>
    public const string LeaderboardForwardedForIpSlug = "forwarded-for-ip";

    /// <summary>
    /// The dimension-folder slug for the referer flat Leaderboard.
    /// </summary>
    public const string LeaderboardRefererSlug = "referer";

    /// <summary>
    /// The dimension-folder slug for the URI flat Leaderboard.
    /// </summary>
    public const string LeaderboardUriSlug = "uri";

    /// <summary>
    /// The dimension-folder slug for the user-agent flat Leaderboard.
    /// </summary>
    public const string LeaderboardUserAgentSlug = "user-agent";

    /// <summary>
    /// The folder segment placeholder used in a Detail Page href for a folderless service, since the href needs
    /// a path segment there regardless (an empty segment would collapse two adjacent slashes). Real ArcGIS
    /// Server folder names aren't expected to collide with this reserved token.
    /// </summary>
    private const string NoFolderSlug = "_";

    /// <summary>
    /// Builds the sidebar's top-level entries.
    /// </summary>
    /// <param name="includedRoots">The Reports project's configured Included Root allow-list.</param>
    /// <returns>The sidebar tree, root entries first.</returns>
    public static IReadOnlyList<SidebarNode> BuildTree(IReadOnlyList<string> includedRoots)
    {
        ArgumentNullException.ThrowIfNull(includedRoots);

        return
        [
            new SidebarNode
            {
                Title = "Summary",
                Children =
                [
                    new SidebarNode
                    {
                        Title = "Successful Requests",
                        Children =
                        [
                            new SidebarNode { Title = "All", Href = LandingPageHref },
                            new SidebarNode { Title = "Last 7 Days", Href = SummaryLast7DaysHref },
                        ],
                    },
                    new SidebarNode { Title = "Complete View", Href = SummaryCompleteViewHref },
                ],
            },
            new SidebarNode
            {
                Title = "Portal",
                Children =
                [
                    BuildPortalMetricGroup("Successful Requests", PortalSuccessfulRequestsSlug),
                    BuildPortalMetricGroup("Failed Requests", PortalFailedRequestsSlug),
                    BuildPortalMetricGroup("Leaderboard: Successful Hits", PortalLeaderboardSuccessfulHitsSlug),
                    BuildPortalMetricGroup("Leaderboard: Failed Hits", PortalLeaderboardFailedHitsSlug),
                    new SidebarNode { Title = "Complete View", Href = PortalCompleteViewHref },
                ],
            },
            new SidebarNode
            {
                Title = "ArcGIS Server",
                Children =
                [
                    .. includedRoots.Order(StringComparer.Ordinal).Select(BuildArcGisServerSiteNode),
                    new SidebarNode { Title = "Complete View", Href = ArcGisServerCompleteViewHref },
                ],
            },
            BuildDeviceNode(FieldmapsSectionTitle, FieldmapsSectionSlug),
            BuildDeviceNode(Survey123SectionTitle, Survey123SectionSlug),
            BuildLeaderboardNode(),
        ];
    }

    /// <summary>
    /// Builds the output-relative path of one flat Leaderboard page.
    /// </summary>
    /// <param name="dimensionSlug">One of this class's <c>Leaderboard*Slug</c> constants.</param>
    /// <param name="range">Which range variant this page shows.</param>
    /// <returns>The page's output-relative path.</returns>
    public static string LeaderboardPageHref(string dimensionSlug, DashboardDateRange range)
    {
        ArgumentNullException.ThrowIfNull(dimensionSlug);

        var rangeSlug = range == DashboardDateRange.Last7Days ? "last-7-days" : "all";
        return $"{LeaderboardSectionSlug}/{dimensionSlug}/{rangeSlug}.html";
    }

    /// <summary>
    /// Builds the output-relative path of one Portal metric page.
    /// </summary>
    /// <param name="metricSlug">One of this class's <c>Portal*Slug</c> constants.</param>
    /// <param name="range">Which range variant this page shows.</param>
    /// <returns>The page's output-relative path.</returns>
    public static string PortalPageHref(string metricSlug, DashboardDateRange range)
    {
        ArgumentNullException.ThrowIfNull(metricSlug);

        var rangeSlug = range == DashboardDateRange.Last7Days ? "last-7-days" : "all";
        return $"{PortalSectionSlug}/{metricSlug}/{rangeSlug}.html";
    }

    /// <summary>
    /// Builds the output-relative path of one Portal item's Detail Page (ticket 16) - reachable only by following
    /// a link from the Portal Complete View, never from the sidebar itself. <paramref name="portalItemId"/> is
    /// sanitized via <see cref="DetailPagePathSegment"/> before use, since - unlike a Root/Site - it is never
    /// filtered against an allow-list and so can carry any character a URI path segment allows.
    /// </summary>
    /// <param name="portalItemId">The Portal item's opaque id.</param>
    /// <returns>The page's output-relative path.</returns>
    public static string PortalItemDetailHref(string portalItemId)
    {
        ArgumentNullException.ThrowIfNull(portalItemId);

        return $"{PortalSectionSlug}/detail/{DetailPagePathSegment.Sanitize(portalItemId)}.html";
    }

    /// <summary>
    /// Builds the output-relative path of one per-device section's (Fieldmaps or Survey123) metric page.
    /// </summary>
    /// <param name="sectionSlug">The section's folder segment, e.g. <see cref="Survey123SectionSlug"/>.</param>
    /// <param name="metricSlug">One of this class's <c>Device*Slug</c> constants.</param>
    /// <param name="range">Which range variant this page shows.</param>
    /// <returns>The page's output-relative path.</returns>
    public static string DevicePageHref(string sectionSlug, string metricSlug, DashboardDateRange range)
    {
        ArgumentNullException.ThrowIfNull(sectionSlug);
        ArgumentNullException.ThrowIfNull(metricSlug);

        var rangeSlug = range == DashboardDateRange.Last7Days ? "last-7-days" : "all";
        return $"{sectionSlug}/{metricSlug}/{rangeSlug}.html";
    }

    /// <summary>
    /// Builds the output-relative path of a per-device section's Complete View page.
    /// </summary>
    /// <param name="sectionSlug">The section's folder segment, e.g. <see cref="Survey123SectionSlug"/>.</param>
    /// <returns>The page's output-relative path.</returns>
    public static string DeviceCompleteViewHref(string sectionSlug)
    {
        ArgumentNullException.ThrowIfNull(sectionSlug);

        return $"{sectionSlug}/complete-view.html";
    }

    /// <summary>
    /// Builds the output-relative path of one device's Detail Page (tickets 21 and 22) - reachable only by
    /// following a link from that section's Complete View, never from the sidebar itself. A device id is a
    /// casefolded GUID (Field Maps) or a lowercase 32-hex string (Survey123), so it is already safe as a file
    /// name; it still goes through <see cref="DetailPagePathSegment"/> as a cheap guard, matching the other
    /// Detail Page hrefs.
    /// </summary>
    /// <param name="sectionSlug">The section's folder segment, e.g. <see cref="Survey123SectionSlug"/>.</param>
    /// <param name="deviceId">The casefolded device id.</param>
    /// <returns>The page's output-relative path.</returns>
    public static string DeviceDetailHref(string sectionSlug, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(sectionSlug);
        ArgumentNullException.ThrowIfNull(deviceId);

        return $"{sectionSlug}/detail/{DetailPagePathSegment.Sanitize(deviceId)}.html";
    }

    /// <summary>
    /// Builds the output-relative path of one ArcGIS Server metric page.
    /// </summary>
    /// <param name="site">The site (Included Root) the page belongs to.</param>
    /// <param name="metricSlug">One of this class's <c>ArcGisServer*Slug</c> constants.</param>
    /// <param name="range">Which range variant this page shows.</param>
    /// <returns>The page's output-relative path.</returns>
    public static string ArcGisServerPageHref(string site, string metricSlug, DashboardDateRange range)
    {
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(metricSlug);

        var rangeSlug = range == DashboardDateRange.Last7Days ? "last-7-days" : "all";
        return $"{ArcGisServerSectionSlug}/{site}/{metricSlug}/{rangeSlug}.html";
    }

    /// <summary>
    /// Builds the output-relative path of one ArcGIS Server service's Detail Page (ticket 13) - reachable only by
    /// following a link from the ArcGIS Server Complete View, never from the sidebar itself.
    /// <paramref name="folder"/>/<paramref name="serviceName"/>/<paramref name="serviceType"/> are each sanitized
    /// via <see cref="DetailPagePathSegment"/> before use - unlike <paramref name="site"/>, which is always an
    /// Included Root drawn from the operator's own allow-list, these three are parsed straight from the request
    /// URI with no allow-list filtering, so they can carry any character a URI path segment allows.
    /// </summary>
    /// <param name="site">The service's site (Included Root).</param>
    /// <param name="folder">The service's folder, or <see langword="null"/> for a folderless service.</param>
    /// <param name="serviceName">The service's name.</param>
    /// <param name="serviceType">The service's type.</param>
    /// <returns>The page's output-relative path.</returns>
#pragma warning disable CC0042 // Four identity components; matches ArcGisServerPageHref's own plain-string parameters just above.
    public static string ArcGisServiceDetailHref(string site, string? folder, string serviceName, string serviceType)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(serviceName);
        ArgumentNullException.ThrowIfNull(serviceType);

        var folderSlug = DetailPagePathSegment.Sanitize(folder ?? NoFolderSlug);
        var serviceNameSlug = DetailPagePathSegment.Sanitize(serviceName);
        var serviceTypeSlug = DetailPagePathSegment.Sanitize(serviceType);
        return $"{ArcGisServerSectionSlug}/{site}/detail/{folderSlug}/{serviceNameSlug}/{serviceTypeSlug}.html";
    }

    private static SidebarNode BuildDeviceNode(string title, string sectionSlug)
    {
        return new SidebarNode
        {
            Title = title,
            Children =
            [
                new SidebarNode
                {
                    Title = "Leaderboard: Hits",
                    Children =
                    [
                        new SidebarNode { Title = "All", Href = DevicePageHref(sectionSlug, DeviceLeaderboardHitsSlug, DashboardDateRange.All) },
                        new SidebarNode { Title = "Last 7 Days", Href = DevicePageHref(sectionSlug, DeviceLeaderboardHitsSlug, DashboardDateRange.Last7Days) },
                    ],
                },
                new SidebarNode { Title = "Complete View", Href = DeviceCompleteViewHref(sectionSlug) },
            ],
        };
    }

    private static SidebarNode BuildLeaderboardNode()
    {
        return new SidebarNode
        {
            Title = "Leaderboard",
            Children =
            [
                BuildLeaderboardDimensionGroup("Forwarded-For IP", LeaderboardForwardedForIpSlug),
                BuildLeaderboardDimensionGroup("Referer", LeaderboardRefererSlug),
                BuildLeaderboardDimensionGroup("URI", LeaderboardUriSlug),
                BuildLeaderboardDimensionGroup("User Agent", LeaderboardUserAgentSlug),
            ],
        };
    }

    private static SidebarNode BuildLeaderboardDimensionGroup(string title, string dimensionSlug)
    {
        return new SidebarNode
        {
            Title = title,
            Children =
            [
                new SidebarNode { Title = "All", Href = LeaderboardPageHref(dimensionSlug, DashboardDateRange.All) },
                new SidebarNode { Title = "Last 7 Days", Href = LeaderboardPageHref(dimensionSlug, DashboardDateRange.Last7Days) },
            ],
        };
    }

    private static SidebarNode BuildPortalMetricGroup(string title, string metricSlug)
    {
        return new SidebarNode
        {
            Title = title,
            Children =
            [
                new SidebarNode { Title = "All", Href = PortalPageHref(metricSlug, DashboardDateRange.All) },
                new SidebarNode { Title = "Last 7 Days", Href = PortalPageHref(metricSlug, DashboardDateRange.Last7Days) },
            ],
        };
    }

    private static SidebarNode BuildArcGisServerSiteNode(string site)
    {
        return new SidebarNode
        {
            Title = site,
            Children =
            [
                BuildArcGisServerMetricGroup("Successful Requests", site, ArcGisServerSuccessfulRequestsSlug),
                BuildArcGisServerMetricGroup("Failed Requests", site, ArcGisServerFailedRequestsSlug),
                BuildArcGisServerMetricGroup("Average Time (Successful)", site, ArcGisServerAverageTimeSuccessfulSlug),
                BuildArcGisServerMetricGroup("Average Time (Failed)", site, ArcGisServerAverageTimeFailedSlug),
                BuildArcGisServerMetricGroup("Leaderboard: Successful Hits", site, ArcGisServerLeaderboardSuccessfulHitsSlug),
                BuildArcGisServerMetricGroup("Leaderboard: Failed Hits", site, ArcGisServerLeaderboardFailedHitsSlug),
                BuildArcGisServerMetricGroup("Leaderboard: Average Time (Successful)", site, ArcGisServerLeaderboardAverageTimeSuccessfulSlug),
                BuildArcGisServerMetricGroup("Leaderboard: Average Time (Failed)", site, ArcGisServerLeaderboardAverageTimeFailedSlug),
            ],
        };
    }

    private static SidebarNode BuildArcGisServerMetricGroup(string title, string site, string metricSlug)
    {
        return new SidebarNode
        {
            Title = title,
            Children =
            [
                new SidebarNode { Title = "All", Href = ArcGisServerPageHref(site, metricSlug, DashboardDateRange.All) },
                new SidebarNode { Title = "Last 7 Days", Href = ArcGisServerPageHref(site, metricSlug, DashboardDateRange.Last7Days) },
            ],
        };
    }
}
