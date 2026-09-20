namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Computes the "../" prefix that turns a root-relative Dashboard href (e.g. <see cref="DashboardSidebar"/>'s own
/// <c>*Href</c> values) into one that resolves correctly from a specific page's own location - every page is a
/// self-contained static file with no root-absolute paths, so the Dashboard also works opened directly from disk
/// (per ADR-0005). Shared by <see cref="PageShellRenderer"/> (the sidebar's own links) and any section builder
/// that embeds a cross-page link directly in a page's body content (e.g. a Complete View row linking to that
/// entity's own Detail Page, per ticket 13).
/// </summary>
public static class RelativeLinkHelper
{
    /// <summary>
    /// Computes the "../" prefix for a page living at <paramref name="pageOutputRelativePath"/>.
    /// </summary>
    /// <param name="pageOutputRelativePath">The linking page's own output-relative path.</param>
    /// <returns>A string of zero or more <c>"../"</c> segments, one per path segment <paramref name="pageOutputRelativePath"/> is nested under the Dashboard root.</returns>
    public static string ComputeRootPrefix(string pageOutputRelativePath)
    {
        ArgumentNullException.ThrowIfNull(pageOutputRelativePath);

        var depth = pageOutputRelativePath.Count(c => c == '/');
        return string.Concat(Enumerable.Repeat("../", depth));
    }
}
