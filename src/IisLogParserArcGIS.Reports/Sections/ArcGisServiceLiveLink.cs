namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds an ArcGIS Server service's own live REST endpoint URL (mirrors <see cref="PortalItemLiveLink"/>'s
/// resolution for Portal items). Every service URL this project ever sees is shaped
/// <c>/site/rest/services/[folder/]service_name/service_type</c> (ticket 12's own parsing rule), with <c>site</c>
/// the top-level path segment on one shared host rather than a per-site hostname - so, unlike Portal, no Web
/// Adaptor name is threaded in here, just the site itself. Used only by the ArcGIS Server Complete View, the one
/// place a service's own live endpoint is linked.
/// </summary>
internal static class ArcGisServiceLiveLink
{
    /// <summary>
    /// Builds the live REST endpoint URL for one ArcGIS Server service.
    /// </summary>
    /// <param name="arcGisServerBaseUrl">The configured ArcGIS Server base host.</param>
    /// <param name="site">The service's site (Included Root).</param>
    /// <param name="folder">The service's folder, or <see langword="null"/> for a folderless service.</param>
    /// <param name="serviceName">The service's name.</param>
    /// <param name="serviceType">The service's type (e.g. <c>MapServer</c>, <c>FeatureServer</c>).</param>
    /// <returns>The service's live REST endpoint URL, with every path segment percent-encoded.</returns>
#pragma warning disable CC0042 // Four independent path segments a REST endpoint URL needs; bundling them would just repackage unrelated values.
    public static string BuildHref(string arcGisServerBaseUrl, string site, string? folder, string serviceName, string serviceType)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(arcGisServerBaseUrl);
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(serviceName);
        ArgumentNullException.ThrowIfNull(serviceType);

        var folderSegment = string.IsNullOrEmpty(folder) ? string.Empty : $"{Uri.EscapeDataString(folder)}/";

        return $"https://{arcGisServerBaseUrl}/{Uri.EscapeDataString(site)}/rest/services/{folderSegment}{Uri.EscapeDataString(serviceName)}/{Uri.EscapeDataString(serviceType)}";
    }
}
