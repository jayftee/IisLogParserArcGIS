using Microsoft.Extensions.Configuration;

namespace IisLogParserArcGIS.Reports.Configuration;

/// <summary>
/// Binds a typed <see cref="ReportsSettings"/> from the executable's single, shared <see cref="IConfiguration"/>
/// (built by the Cli project's own configuration factory from its one <c>appsettings.json</c> - the Reports
/// project has no configuration file of its own), applying the repository's documented defaults for any value
/// left blank.
/// </summary>
public static class ReportsConfigurationFactory
{
    private const string DefaultLocalTimeZone = "UTC";
    private const string DefaultOutputDirectory = "Dashboard";

    /// <summary>
    /// Binds <see cref="ReportsSettings"/> from the given configuration, substituting the documented default for
    /// any value left blank.
    /// </summary>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <returns>The bound, defaulted settings.</returns>
    public static ReportsSettings BindReportsSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration.Get<ReportsSettings>() ?? new ReportsSettings();

        settings.LocalTimeZone = Coalesce(settings.LocalTimeZone, DefaultLocalTimeZone);
        settings.OutputDirectory = Coalesce(settings.OutputDirectory, DefaultOutputDirectory);
        settings.IncludedRoots ??= [];

        return settings;
    }

    private static string Coalesce(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
