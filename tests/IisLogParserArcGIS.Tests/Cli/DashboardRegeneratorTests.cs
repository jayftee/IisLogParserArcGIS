using IisLogParserArcGIS.Cli;
using IisLogParserArcGIS.Configuration;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Tests.Cli;

public class DashboardRegeneratorTests
{
    private const string SentinelFileName = "sentinel.txt";

    // Each is resolved against a base directory that (like AppContext.BaseDirectory) ends in a separator.
    [Theory]
    [InlineData(".")]
    [InlineData("./")]
    [InlineData("sub/..")]
    [InlineData("{base}")]
    [InlineData("{base-no-trailing-separator}")]
    [InlineData("{base-upper-case}")]
    public void Regenerate_OutputDirectoryResolvesToTheBaseDirectory_RefusesAndLeavesItUntouched(string configuredOutputDirectory)
    {
        ArgumentNullException.ThrowIfNull(configuredOutputDirectory);
        using var workspace = new CliTestWorkspace();
        var outputDirectory = configuredOutputDirectory
            .Replace("{base}", workspace.BaseDirectory, StringComparison.Ordinal)
            .Replace("{base-no-trailing-separator}", workspace.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal)
            .Replace("{base-upper-case}", workspace.BaseDirectory.ToUpperInvariant(), StringComparison.Ordinal);
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = outputDirectory });
        File.WriteAllText(Path.Combine(workspace.BaseDirectory, SentinelFileName), "keep me");

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        var error = workspace.Error.ToString();
        Assert.StartsWith("Failed to regenerate the Dashboard: Refusing to use '", error, StringComparison.Ordinal);
        Assert.Contains("' as the Dashboard output directory: it resolves to the executable's own directory, which this run would delete recursively.", error, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, SentinelFileName)));
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "appsettings.json")));
    }

    [Fact]
    public void Regenerate_RelativeOutputDirectory_IsResolvedAgainstTheBaseDirectoryAndTheDashboardIsGenerated()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = "MyDashboard" });

        var exitCode = Regenerate(workspace);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, workspace.Error.ToString());
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "MyDashboard", "index.html")));
    }

    [Fact]
    public void Regenerate_AbsoluteOutputDirectory_IsUsedAsIs()
    {
        using var workspace = new CliTestWorkspace();
        var absoluteOutputDirectory = Path.Combine(workspace.DataDirectory, "AbsoluteDashboard");
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = absoluteOutputDirectory });

        var exitCode = Regenerate(workspace);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(absoluteOutputDirectory, "index.html")));
    }

    [Fact]
    public void Regenerate_InvalidLocalTimeZone_ReturnsOneWithTheDashboardFailureMessage()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["LocalTimeZone"] = "Not/AZone" });

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to regenerate the Dashboard: ", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("Not/AZone", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Regenerate_OutputDirectoryIsADriveRoot_ReturnsOneWithTheDashboardFailureMessage()
    {
        using var workspace = new CliTestWorkspace();
        var driveRoot = Path.GetPathRoot(workspace.BaseDirectory)!;
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = driveRoot });

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        Assert.Contains("Failed to regenerate the Dashboard: Refusing to use", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("it contains the executable's own directory '", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Regenerate_OutputDirectoryParentIsAFile_ReturnsOneWithTheDashboardFailureMessage()
    {
        using var workspace = new CliTestWorkspace();
        var fileInTheWay = Path.Combine(workspace.DataDirectory, "not-a-directory");
        File.WriteAllText(fileInTheWay, "x");
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = Path.Combine(fileInTheWay, "Dashboard") });

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to regenerate the Dashboard: ", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    // The workspace is root/{base, logs-in, data}; the database lives in data, the default log output in base/Logs.
    [Theory]
    [InlineData("..")]
    [InlineData("../")]
    [InlineData("{root}")]
    [InlineData("{root-upper-case}")]
    public void Regenerate_OutputDirectoryContainsTheBaseDirectory_RefusesAndDeletesNothing(string configuredOutputDirectory)
    {
        ArgumentNullException.ThrowIfNull(configuredOutputDirectory);
        using var workspace = new CliTestWorkspace();
        var root = Path.GetDirectoryName(workspace.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!;
        var outputDirectory = configuredOutputDirectory
            .Replace("{root}", root, StringComparison.Ordinal)
            .Replace("{root-upper-case}", root.ToUpperInvariant(), StringComparison.Ordinal);
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = outputDirectory });
        var logSourceFile = workspace.WriteLogFile("u_ex260501_x_1.log", "keep me");

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        Assert.Contains("it contains the executable's own directory '", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "appsettings.json")));
        Assert.True(File.Exists(workspace.DatabasePath()));
        Assert.True(File.Exists(logSourceFile));
    }

    [Fact]
    public void Regenerate_OutputDirectoryContainsTheDatabase_RefusesAndKeepsTheDatabase()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = workspace.DataDirectory });

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        Assert.Contains("it contains the aggregate database '", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(workspace.DatabasePath()));
    }

    [Theory]
    [InlineData("Logs")]
    [InlineData("./Logs/")]
    [InlineData("Logs/nested/..")]
    public void Regenerate_OutputDirectoryIsTheLogOutputDirectory_RefusesAndKeepsItsContents(string configuredOutputDirectory)
    {
        ArgumentNullException.ThrowIfNull(configuredOutputDirectory);
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = configuredOutputDirectory });
        var logFile = Path.Combine(Directory.CreateDirectory(Path.Combine(workspace.BaseDirectory, "Logs")).FullName, "iislogparser.log");
        File.WriteAllText(logFile, "keep me");

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        Assert.Contains("it resolves to the log output directory, which this run would delete recursively.", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(logFile));
    }

    [Fact]
    public void Regenerate_OutputDirectoryContainsAConfiguredLogOutputDirectoryElsewhere_Refuses()
    {
        using var workspace = new CliTestWorkspace();
        var outputDirectory = Path.Combine(workspace.DataDirectory, "shared");
        var logDirectory = Path.Combine(outputDirectory, "logs");
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = outputDirectory, ["LogOutputDirectory"] = logDirectory });
        var logFile = Path.Combine(Directory.CreateDirectory(logDirectory).FullName, "iislogparser.log");
        File.WriteAllText(logFile, "keep me");

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        Assert.Contains("it contains the log output directory '", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(logFile));
    }

    [Fact]
    public void Regenerate_OutputDirectoryIsTheLogSourceDirectory_RefusesAndKeepsItsContents()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = workspace.LogSourceDirectory });
        var logSourceFile = workspace.WriteLogFile("u_ex260501_x_1.log", "keep me");

        var exitCode = Regenerate(workspace, logSourceDirectory: workspace.LogSourceDirectory);

        Assert.Equal(1, exitCode);
        Assert.Contains("it resolves to the log source directory, which this run would delete recursively.", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(logSourceFile));
    }

    [Fact]
    public void Regenerate_OutputDirectoryContainsTheLogSourceDirectory_RefusesAndKeepsItsContents()
    {
        using var workspace = new CliTestWorkspace();
        var archiveDirectory = workspace.CreateDirectoryUnderRoot("archive");
        var nestedLogSourceDirectory = Directory.CreateDirectory(Path.Combine(archiveDirectory, "iis-logs")).FullName;
        var logSourceFile = Path.Combine(nestedLogSourceDirectory, "u_ex260501_x_1.log");
        File.WriteAllText(logSourceFile, "keep me");
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = archiveDirectory });

        var exitCode = Regenerate(workspace, logSourceDirectory: nestedLogSourceDirectory);

        Assert.Equal(1, exitCode);
        Assert.Contains("it contains the log source directory '", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(logSourceFile));
    }

    [Fact]
    public void Regenerate_LogOutputDirectoryIsNotAValidPath_ReturnsOneWithTheDashboardFailureMessageInsteadOfCrashing()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["LogOutputDirectory"] = "bad\0directory" });

        var exitCode = Regenerate(workspace);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to regenerate the Dashboard: ", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(workspace.BaseDirectory, "Dashboard")));
    }

    [Fact]
    public void Regenerate_OutputDirectoryOnlySharesANamePrefixWithTheBaseDirectory_IsNotMistakenForIt()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = "../base-dashboard" });

        var exitCode = Regenerate(workspace);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, workspace.Error.ToString());
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "..", "base-dashboard", "index.html")));
    }

    [Fact]
    public void Regenerate_OutputDirectoryIsInsideAProtectedDirectory_IsAllowed()
    {
        using var workspace = new CliTestWorkspace();
        var outputDirectory = Path.Combine(workspace.DataDirectory, "dashboard");
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = outputDirectory });

        var exitCode = Regenerate(workspace, logSourceDirectory: workspace.LogSourceDirectory);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(outputDirectory, "index.html")));
        Assert.True(File.Exists(workspace.DatabasePath()));
    }

    private static SqliteConnection OpenDatabase(CliTestWorkspace workspace)
    {
        var connection = SqliteConnectionFactory.Open(workspace.DatabasePath());
        AggregateDatabaseSchema.EnsureCreated(connection);
        return connection;
    }

    private static int Regenerate(CliTestWorkspace workspace, string? logSourceDirectory = null)
    {
        var configuration = AppConfigurationFactory.Build(workspace.BaseDirectory, "Production");
        using var connection = OpenDatabase(workspace);
        return new DashboardRegenerator(workspace.Environment()).Regenerate(connection, configuration, logSourceDirectory);
    }
}
