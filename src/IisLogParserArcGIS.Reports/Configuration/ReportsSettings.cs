namespace IisLogParserArcGIS.Reports.Configuration;

/// <summary>
/// Configuration values bound from the executable's single, shared <c>appsettings.json</c>, with the
/// <c>Development</c> overlay applied when active - the Reports project has no configuration file of its own.
/// </summary>
public sealed class ReportsSettings
{
    /// <summary>
    /// Gets or sets the <see cref="TimeZoneInfo"/>-compatible identifier this project resolves "today" and
    /// "this year" from at the start of every Regeneration Run.
    /// </summary>
    public string LocalTimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the flat, lowercase allow-list of Root/Site names shown in the Dashboard's ArcGIS Server
    /// section. A Root present in the aggregate database but absent from this list is silently excluded from
    /// every report.
    /// </summary>
    public IReadOnlyList<string> IncludedRoots { get; set; } = [];

    /// <summary>
    /// Gets or sets this deployment's ArcGIS Portal base URL: the host, plus whatever path prefix (typically a
    /// Web Adaptor name) is actually needed to reach Portal's own web pages there - e.g. <c>portal.example.com</c>
    /// when Portal sits at the domain root, or <c>geospatial.example.gov/portal</c> when it doesn't. Not a full
    /// URI on its own - no scheme, since
    /// <see cref="IisLogParserArcGIS.Reports.Sections.PortalItemLiveLink.BuildHref"/> always prepends
    /// <c>https://</c> - used to link every Portal Item reference in the Dashboard to that item's live page on
    /// Portal.
    /// </summary>
#pragma warning disable CA1056 // A host-plus-path-prefix substituted into a URL template, not a well-formed System.Uri on its own.
    public string PortalBaseUrl { get; set; } = string.Empty;
#pragma warning restore CA1056

    /// <summary>
    /// Gets or sets this deployment's ArcGIS Server base host: unlike <see cref="PortalBaseUrl"/>, every site
    /// (Included Root) is a top-level path segment on this one shared host rather than a host of its own - e.g.
    /// <c>geospatial.example.gov</c>, with a service then reachable at
    /// <c>https://geospatial.example.gov/{site}/rest/services/...</c>. Not a full URI on its own - no scheme,
    /// since <see cref="IisLogParserArcGIS.Reports.Sections.ArcGisServiceLiveLink.BuildHref"/> always prepends
    /// <c>https://</c> - used to link every ArcGIS Server service reference in the Dashboard to that service's own
    /// live REST endpoint.
    /// </summary>
#pragma warning disable CA1056 // A bare host substituted into a URL template, not a well-formed System.Uri on its own.
    public string ArcGisServerBaseUrl { get; set; } = string.Empty;
#pragma warning restore CA1056

    /// <summary>
    /// Gets or sets the directory (relative to the executable, or absolute) each Regeneration Run (re)generates
    /// the Dashboard into.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;
}
