using IisLogParserArcGIS.Cli;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Tests.TestSupport;

namespace IisLogParserArcGIS.Tests.Cli;

public class RegenerateRunnerTests
{
    [Fact]
    public void Run_ExistingDatabase_RegeneratesTheDashboardAndReturnsZero()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var databasePath = CreateEmptyAggregateDatabase(workspace);

        var exitCode = Run(workspace, databasePath);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, workspace.Error.ToString());
        Assert.True(File.Exists(Path.Combine(workspace.DashboardDirectory, "index.html")));
    }

    [Fact]
    public void Run_PastYear_RegeneratesTheDashboardForThatYear()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var databasePath = CreateEmptyAggregateDatabase(workspace);

        var exitCode = Run(workspace, new RegenerateArguments { InputDatabasePath = databasePath, OutputDirectory = workspace.DashboardDirectory, Year = 2025 });

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, workspace.Error.ToString());
        Assert.True(File.Exists(Path.Combine(workspace.DashboardDirectory, "index.html")));
    }

    [Fact]
    public void Run_TwoYearsIntoTwoOutputDirectories_LeavesBothDashboardsInPlace()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var databasePath = CreateEmptyAggregateDatabase(workspace);
        var firstDashboard = workspace.CreateDirectoryUnderRoot("dashboard-2025");
        var secondDashboard = workspace.CreateDirectoryUnderRoot("dashboard-2026");

        var firstExitCode = Run(workspace, new RegenerateArguments { InputDatabasePath = databasePath, OutputDirectory = firstDashboard, Year = 2025 });
        var secondExitCode = Run(workspace, new RegenerateArguments { InputDatabasePath = databasePath, OutputDirectory = secondDashboard, Year = 2026 });

        Assert.Equal(0, firstExitCode);
        Assert.Equal(0, secondExitCode);
        Assert.True(File.Exists(Path.Combine(firstDashboard, "index.html")));
        Assert.True(File.Exists(Path.Combine(secondDashboard, "index.html")));
    }

    [Fact]
    public void Run_DatabaseDoesNotExist_ReturnsOneWithTheValidationMessageAndDoesNotCreateTheFile()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var databasePath = workspace.DatabasePath("missing.sqlite");

        var exitCode = Run(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            $"The input database path '{databasePath}' does not exist. The regenerate verb reads an existing aggregate database - it never creates one.{Environment.NewLine}",
            workspace.Error.ToString());
        Assert.False(File.Exists(databasePath));
        Assert.False(Directory.Exists(workspace.DashboardDirectory));
    }

    [Fact]
    public void Run_EmptyDatabasePath_ReturnsOneWithTheValidationMessage()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();

        var exitCode = Run(workspace, string.Empty);

        Assert.Equal(1, exitCode);
        Assert.Equal($"The input database path must not be empty.{Environment.NewLine}", workspace.Error.ToString());
    }

    [Fact]
    public void Run_MissingAppSettings_ReturnsOneWithTheInitializationMessage()
    {
        using var workspace = new CliTestWorkspace();
        var databasePath = CreateEmptyAggregateDatabase(workspace);

        var exitCode = Run(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to initialize configuration or the input database: ", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(workspace.DashboardDirectory));
    }

    [Fact]
    public void Run_FileThatIsNotADatabase_ReturnsOneWithTheInitializationMessage()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var databasePath = workspace.DatabasePath();
        File.WriteAllText(databasePath, "this is definitely not a SQLite database, just some text long enough to be rejected by the format check.");

        var exitCode = Run(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to initialize configuration or the input database: ", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_HonorsTheEnvironmentNameOverlay()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        workspace.WriteOverlay("Development", new Dictionary<string, object?> { ["LocalTimeZone"] = "Not/AZone" });
        var databasePath = CreateEmptyAggregateDatabase(workspace);

        var exitCode = new RegenerateRunner(workspace.Environment("Development"))
            .Run(new RegenerateArguments { InputDatabasePath = databasePath, OutputDirectory = workspace.DashboardDirectory, Year = 2026 });

        Assert.Equal(1, exitCode);
        Assert.Contains("Not/AZone", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_DashboardOutputResolvesToTheBaseDirectory_ReturnsOneAndKeepsTheBaseDirectory()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var databasePath = CreateEmptyAggregateDatabase(workspace);

        var exitCode = Run(workspace, databasePath, workspace.BaseDirectory);

        Assert.Equal(1, exitCode);
        Assert.Contains("Refusing to use", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "appsettings.json")));
    }

    private static string CreateEmptyAggregateDatabase(CliTestWorkspace workspace)
    {
        var databasePath = workspace.DatabasePath();
        using var connection = SqliteConnectionFactory.Open(databasePath);
        AggregateDatabaseSchema.EnsureCreated(connection);
        return databasePath;
    }

    private static int Run(CliTestWorkspace workspace, string inputDatabasePath, string? outputDirectory = null)
    {
        var arguments = new RegenerateArguments
        {
            InputDatabasePath = inputDatabasePath,
            OutputDirectory = outputDirectory ?? workspace.DashboardDirectory,
            Year = 2026,
        };

        return Run(workspace, arguments);
    }

    private static int Run(CliTestWorkspace workspace, RegenerateArguments arguments)
    {
        return new RegenerateRunner(workspace.Environment()).Run(arguments);
    }
}
