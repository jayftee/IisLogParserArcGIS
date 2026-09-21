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
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "Dashboard", "index.html")));
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
        Assert.False(Directory.Exists(Path.Combine(workspace.BaseDirectory, "Dashboard")));
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
        Assert.False(Directory.Exists(Path.Combine(workspace.BaseDirectory, "Dashboard")));
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
        workspace.WriteOverlay("Development", new Dictionary<string, object?> { ["OutputDirectory"] = "DevDashboard" });
        var databasePath = CreateEmptyAggregateDatabase(workspace);

        var exitCode = new RegenerateRunner(workspace.Environment("Development")).Run(new RegenerateArguments { InputDatabasePath = databasePath });

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "DevDashboard", "index.html")));
        Assert.False(Directory.Exists(Path.Combine(workspace.BaseDirectory, "Dashboard")));
    }

    [Fact]
    public void Run_DashboardOutputResolvesToTheBaseDirectory_ReturnsOneAndKeepsTheBaseDirectory()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = "." });
        var databasePath = CreateEmptyAggregateDatabase(workspace);

        var exitCode = Run(workspace, databasePath);

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

    private static int Run(CliTestWorkspace workspace, string inputDatabasePath)
    {
        return new RegenerateRunner(workspace.Environment()).Run(new RegenerateArguments { InputDatabasePath = inputDatabasePath });
    }
}
