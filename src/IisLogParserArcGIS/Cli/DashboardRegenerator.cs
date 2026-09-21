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
    /// Regenerates the Dashboard from the aggregate database behind <paramref name="connection"/>. A Regeneration
    /// Run deletes its output directory recursively, so the run is refused when that directory is, or contains,
    /// the executable's own directory, the aggregate database, the log output directory, or
    /// <paramref name="logSourceDirectory"/>.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database.</param>
    /// <param name="configuration">The executable's built configuration, the Reports settings are bound from.</param>
    /// <param name="logSourceDirectory">The IIS log directory of the current Harvest Run, if there is one.</param>
    /// <returns><c>0</c> on success; <c>1</c> after writing the failure message to the error writer.</returns>
    public int Regenerate(SqliteConnection connection, IConfiguration configuration, string? logSourceDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(configuration);

        try
        {
            var reportsSettings = ReportsConfigurationFactory.BindReportsSettings(configuration);
            var logOutputDirectory = AppConfigurationFactory.BindAppSettings(configuration).LogOutputDirectory;
            var reportsOutputDirectory = RelativePathResolver.Resolve(reportsSettings.OutputDirectory, _environment.BaseDirectory);

            var protectedPaths = new List<ProtectedPath>
            {
                new("the executable's own directory", _environment.BaseDirectory),
                new("the aggregate database", connection.DataSource),
                new("the log output directory", RelativePathResolver.Resolve(logOutputDirectory, _environment.BaseDirectory)),
            };

            if (logSourceDirectory is not null)
            {
                protectedPaths.Add(new ProtectedPath("the log source directory", logSourceDirectory));
            }

            EnsureNothingProtectedIsDeleted(reportsOutputDirectory, protectedPaths);

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

    private static void EnsureNothingProtectedIsDeleted(string outputDirectory, IEnumerable<ProtectedPath> protectedPaths)
    {
        var fullOutputDirectory = Normalize(outputDirectory);

        foreach (var (description, path) in protectedPaths)
        {
            var fullPath = Normalize(path);

            if (string.Equals(fullOutputDirectory, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Refusing to use '{fullOutputDirectory}' as the Dashboard output directory: it resolves to {description}, which this run would delete recursively.",
                    nameof(outputDirectory));
            }

            if (IsInside(fullPath, fullOutputDirectory))
            {
                throw new ArgumentException(
                    $"Refusing to use '{fullOutputDirectory}' as the Dashboard output directory: it contains {description} '{fullPath}', which this run would delete recursively.",
                    nameof(outputDirectory));
            }
        }
    }

    private static string Normalize(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static bool IsInside(string path, string directory)
    {
        var directoryPrefix = Path.EndsInDirectorySeparator(directory) ? directory : directory + Path.DirectorySeparatorChar;

        return path.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ProtectedPath(string Description, string Path);
}
