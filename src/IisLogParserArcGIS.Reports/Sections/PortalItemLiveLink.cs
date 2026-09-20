namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Builds a Portal item's own live page URL on Portal (per ticket 05's resolution: no title resolution, no live
/// Portal REST call made at generation time). Shared by the Portal Complete View (ticket 15) and Portal item
/// Detail Page (ticket 16), which both show the identical link.
/// </summary>
internal static class PortalItemLiveLink
{
    /// <summary>
    /// Builds the live-Portal-page URL for one Portal item.
    /// </summary>
    /// <param name="portalBaseUrl">The configured Portal Web Adaptor base URL.</param>
    /// <param name="portalItemId">The Portal item's opaque id.</param>
    /// <returns>The item's live page URL, with <paramref name="portalItemId"/> percent-encoded into the query string.</returns>
    public static string BuildHref(string portalBaseUrl, string portalItemId)
    {
        ArgumentNullException.ThrowIfNull(portalBaseUrl);
        ArgumentNullException.ThrowIfNull(portalItemId);

        return $"https://{portalBaseUrl}/home/item.html?id={Uri.EscapeDataString(portalItemId)}";
    }
}
