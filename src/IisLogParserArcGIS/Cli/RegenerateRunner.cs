using IisLogParserArcGIS.Configuration;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Schema;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// Runs the <c>regenerate</c> verb: validates its argument, reads configuration, opens the existing aggregate
/// database and regenerates the Dashboard from it - never touching log discovery, parsing or aggregation.
/// </summary>
public sealed class RegenerateRunner
{
    private readonly RunEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegenerateRunner"/> class.
    /// </summary>
    /// <param name="environment">The process environment this run reads from and reports to.</param>
    public RegenerateRunner(RunEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        _environment = environment;
    }

    /// <summary>
    /// Runs the <c>regenerate</c> verb.
    /// </summary>
    /// <param name="arguments">The raw arguments parsed from the command line.</param>
    /// <returns><c>0</c> on success; <c>1</c> after writing the failure message to the error writer.</returns>
    public int Run(RegenerateArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        ParsedRegenerateArguments parsedArguments;
        try
        {
            parsedArguments = CliArgumentValidator.Validate(arguments);
        }
        catch (CliArgumentValidationException ex)
        {
            _environment.Error.WriteLine(ex.Message);
            return 1;
        }

        try
        {
            var configuration = AppConfigurationFactory.Build(_environment.BaseDirectory, _environment.EnvironmentName);

            using var connection = SqliteConnectionFactory.Open(parsedArguments.InputDatabasePath);
            AggregateDatabaseSchema.EnsureCreated(connection);

            return new DashboardRegenerator(_environment).Regenerate(connection, configuration);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FormatException or IOException or UnauthorizedAccessException or SqliteException)
        {
            _environment.Error.WriteLine($"Failed to initialize configuration or the input database: {ex.Message}");
            return 1;
        }
    }
}
