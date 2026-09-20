using Microsoft.Extensions.Configuration;

namespace IisLogParserArcGIS.Configuration;

/// <summary>
/// Builds the executable's single, shared <see cref="IConfiguration"/> and binds it to a typed
/// <see cref="AppSettings"/>, applying the repository's documented defaults for any value left blank. The same
/// built <see cref="IConfiguration"/> is also handed to
/// <see cref="Reports.Configuration.ReportsConfigurationFactory.BindReportsSettings"/> - one executable, one
/// <c>appsettings.json</c>, read once.
/// </summary>
public static class AppConfigurationFactory
{
    private const string DefaultLocalTimeZone = "UTC";
    private const string DefaultLogOutputDirectory = "Logs";
    private const string DefaultLogLevel = "Warning";
    private const string DefaultPortalWebAdaptorName = "portal";
    private const string EnvironmentVariablePrefix = "IISLOGPARSER_";

    /// <summary>
    /// Builds the configuration from <c>appsettings.json</c>, overlaid with <c>appsettings.{environmentName}.json</c>
    /// when present, and finally with any <c>IISLOGPARSER_</c>-prefixed environment variable (e.g.
    /// <c>IISLOGPARSER_LocalTimeZone</c>) - so a single run can override a setting without editing a file, and
    /// only variables carrying that prefix can ever affect the configuration.
    /// </summary>
    /// <param name="basePath">The directory containing the <c>appsettings*.json</c> files.</param>
    /// <param name="environmentName">The active environment name (e.g. <c>Development</c>, <c>Production</c>).</param>
    /// <returns>The built configuration.</returns>
    public static IConfiguration Build(string basePath, string environmentName)
    {
#pragma warning disable CC0098 // ConfigurationBuilder fluent API; chaining is the intended usage.
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(EnvironmentVariablePrefix)
            .Build();
#pragma warning restore CC0098
    }

    /// <summary>
    /// Binds <see cref="AppSettings"/> from the given configuration, substituting the documented default for
    /// any value left blank.
    /// </summary>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <returns>The bound, defaulted settings.</returns>
    public static AppSettings BindAppSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration.Get<AppSettings>() ?? new AppSettings();

        settings.LocalTimeZone = Coalesce(settings.LocalTimeZone, DefaultLocalTimeZone);
        settings.LogOutputDirectory = Coalesce(settings.LogOutputDirectory, DefaultLogOutputDirectory);
        settings.LogLevel = Coalesce(settings.LogLevel, DefaultLogLevel);
        settings.PortalWebAdaptorName = Coalesce(settings.PortalWebAdaptorName, DefaultPortalWebAdaptorName);

        return settings;
    }

    private static string Coalesce(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
