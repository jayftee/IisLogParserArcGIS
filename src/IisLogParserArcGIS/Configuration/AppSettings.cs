namespace IisLogParserArcGIS.Configuration;

/// <summary>
/// Configuration values bound from <c>appsettings.json</c>, with the <c>Development</c> overlay applied when active.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the <see cref="TimeZoneInfo"/>-compatible identifier used to derive local date/time values
    /// from the UTC timestamps recorded in the log files.
    /// </summary>
    public string LocalTimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory (relative to the executable, or absolute) that rolling log files are written to.
    /// </summary>
    public string LogOutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum Serilog level, by name (e.g. <c>Warning</c>, <c>Debug</c>), at which application
    /// code logs.
    /// </summary>
    public string LogLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets this deployment's ArcGIS Portal Web Adaptor name, used to recognize Portal item-access
    /// requests in <c>cs-uri-stem</c> values.
    /// </summary>
    public string PortalWebAdaptorName { get; set; } = string.Empty;
}
