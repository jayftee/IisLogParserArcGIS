using IisLogParserArcGIS.Configuration;
using IisLogParserArcGIS.Reports;
using IisLogParserArcGIS.Reports.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The Dashboard-regeneration step shared by the <c>regenerate</c> and <c>harvest-regenerate</c> verbs: binds the
/// Reports settings, resolves and guards the Dashboard output directory, runs the Regeneration Run, and turns any
/// operational failure into an operator-facing message and a non-zero exit code.
/// </summary>
public sealed class DashboardRegenerator
{
    private readonly RunEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardRegenerator"/> class.
    /// </summary>
    /// <param name="environment">The process environment this run reads from and reports to.</param>
    public DashboardRegenerator(RunEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        _environment = environment;
    }

    /// <summary>
    /// Regenerates the Dashboard from the aggregate database behind <paramref name="connection"/>.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="configuration">The executable's built configuration, the Reports settings are bound from.</param>
    /// <returns><c>0</c> on success; <c>1</c> after writing the failure message to the error writer.</returns>
    public int Regenerate(SqliteConnection connection, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(configuration);

        try
        {
            var reportsSettings = ReportsConfigurationFactory.BindReportsSettings(configuration);
            var reportsOutputDirectory = ResolveReportsOutputDirectory(reportsSettings.OutputDirectory, _environment.BaseDirectory);

            RegenerationRun.Run(connection, reportsSettings, _environment.TimeProvider, reportsOutputDirectory);
            return 0;
        }
        catch (Exception ex) when (ex is FileNotFoundException or FormatException or IOException or UnauthorizedAccessException
            or TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException or SqliteException)
        {
            _environment.Error.WriteLine($"Failed to regenerate the Dashboard: {ex.Message}");
            return 1;
        }
    }

    private static string ResolveReportsOutputDirectory(string outputDirectory, string baseDirectory)
    {
        var resolved = RelativePathResolver.Resolve(outputDirectory, baseDirectory);
        var fullResolved = Path.TrimEndingDirectorySeparator(Path.GetFullPath(resolved));
        var fullBaseDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(baseDirectory));

        if (string.Equals(fullResolved, fullBaseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Refusing to use '{fullResolved}' as the Dashboard output directory: it resolves to the executable's own directory, which this run would delete recursively.",
                nameof(outputDirectory));
        }

        return resolved;
    }
}
