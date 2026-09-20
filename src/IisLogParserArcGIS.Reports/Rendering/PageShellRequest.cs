namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Everything <see cref="PageShellRenderer"/> needs to wrap one page's own body content in the shared Dashboard
/// shell (header, sidebar, footer).
/// </summary>
public sealed record PageShellRequest
{
    /// <summary>
    /// Gets the page's <c>&lt;title&gt;</c> text.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets this page's own output-relative path (e.g. <c>summary/complete-view.html</c>), used to compute
    /// relative links back to the Dashboard root (assets, the brand link) from wherever this page lands.
    /// </summary>
    public required string OutputRelativePath { get; init; }

    /// <summary>
    /// Gets the sidebar entry's <see cref="SidebarNode.Href"/> to highlight as active. Distinct from
    /// <see cref="OutputRelativePath"/> for pages that duplicate another page's content (e.g. <c>index.html</c>
    /// highlights the same entry as the Summary → Successful Requests → All page it duplicates).
    /// </summary>
    public required string ActiveHref { get; init; }

    /// <summary>
    /// Gets this page's own body content, as raw HTML.
    /// </summary>
    public required string BodyHtml { get; init; }

    /// <summary>
    /// Gets a value indicating whether this page draws a Google Chart, so the shell loads the Google Charts CDN
    /// loader script only on pages that actually need it.
    /// </summary>
    public bool RequiresGoogleCharts { get; init; }

    /// <summary>
    /// Gets the instant this Regeneration Run started, shown in the footer's "Created on" line.
    /// </summary>
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Gets the Reports project's configured Included Root allow-list, used to build this page's sidebar's
    /// ArcGIS Server group (one sub-group per Root).
    /// </summary>
    public required IReadOnlyList<string> IncludedRoots { get; init; }
}
