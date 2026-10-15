using IisLogParserArcGIS.Cli;
using IisLogParserArcGIS.Configuration;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Reports;
using IisLogParserArcGIS.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Tests.Cli;

public class DashboardRegeneratorTests
{
    private const string SentinelFileName = "sentinel.txt";
    private const int Year = 2026;

    // The base directory ends in a separator, like AppContext.BaseDirectory.
    [Theory]
    [InlineData("{base}")]
    [InlineData("{base}.")]
    [InlineData("{base}sub/..")]
    [InlineData("{base-no-trailing-separator}")]
    [InlineData("{base-upper-case}")]
    public void Regenerate_OutputDirectoryResolvesToTheBaseDirectory_RefusesAndLeavesItUntouched(string requestedOutputDirectory)
    {
        ArgumentNullException.ThrowIfNull(requestedOutputDirectory);
        using var workspace = new CliTestWorkspace();
        var outputDirectory = requestedOutputDirectory
            .Replace("{base-no-trailing-separator}", workspace.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal)
            .Replace("{base-upper-case}", workspace.BaseDirectory.ToUpperInvariant(), StringComparison.Ordinal)
            .Replace("{base}", workspace.BaseDirectory, StringComparison.Ordinal);
        workspace.WriteAppSettings();
        File.WriteAllText(Path.Combine(workspace.BaseDirectory, SentinelFileName), "keep me");

        var exitCode = Regenerate(workspace, outputDirectory);

        Assert.Equal(1, exitCode);
        var error = workspace.Error.ToString();
        Assert.StartsWith("Failed to regenerate the Dashboard: Refusing to use '", error, StringComparison.Ordinal);
        Assert.Contains("' as the Dashboard output directory: it resolves to the executable's own directory, which this run would delete recursively.", error, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, SentinelFileName)));
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "appsettings.json")));
    }

    [Fact]
    public void Regenerate_AbsoluteOutputDirectory_IsUsedAsIsAndTheDashboardIsGenerated()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var outputDirectory = Path.Combine(workspace.DataDirectory, "AbsoluteDashboard");

        var exitCode = Regenerate(workspace, outputDirectory);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, workspace.Error.ToString());
        Assert.True(File.Exists(Path.Combine(outputDirectory, "index.html")));
        Assert.True(File.Exists(Path.Combine(outputDirectory, RegenerationRun.MarkerFileName)));
    }

    [Fact]
    public void Regenerate_ConfigurationHoldingALegacyOutputDirectoryKey_IgnoresIt()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = Path.Combine(workspace.DataDirectory, "FromConfiguration") });
        var outputDirectory = Path.Combine(workspace.DataDirectory, "FromArgument");

        var exitCode = Regenerate(workspace, outputDirectory);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(outputDirectory, "index.html")));
        Assert.False(Directory.Exists(Path.Combine(workspace.DataDirectory, "FromConfiguration")));
    }

    [Fact]
    public void Regenerate_NonEmptyOutputDirectoryWithoutTheMarker_RefusesAndKeepsItsContents()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var outputDirectory = Directory.CreateDirectory(Path.Combine(workspace.DataDirectory, "someone-elses-files")).FullName;
        var precious = Path.Combine(outputDirectory, "precious.txt");
        File.WriteAllText(precious, "keep me");

        var exitCode = Regenerate(workspace, outputDirectory);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to regenerate the Dashboard: Refusing to use '", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains(RegenerationRun.MarkerFileName, workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Equal("keep me", File.ReadAllText(precious));
    }

    [Fact]
    public void Regenerate_YearAfterTheCurrentOne_ReturnsOneWithTheDashboardFailureMessageAndWritesNothing()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var outputDirectory = DefaultOutputDirectory(workspace);
        var configuration = AppConfigurationFactory.Build(workspace.BaseDirectory, "Production");
        using var connection = OpenDatabase(workspace);

        var exitCode = new DashboardRegenerator(workspace.Environment()).Regenerate(connection, configuration, outputDirectory, 2999);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to regenerate the Dashboard: ", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("2999", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(outputDirectory));
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
        workspace.WriteAppSettings();
        var driveRoot = Path.GetPathRoot(workspace.BaseDirectory)!;

        var exitCode = Regenerate(workspace, driveRoot);

        Assert.Equal(1, exitCode);
        Assert.Contains("Failed to regenerate the Dashboard: Refusing to use", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("it contains the executable's own directory '", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Regenerate_OutputDirectoryParentIsAFile_ReturnsOneWithTheDashboardFailureMessage()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var fileInTheWay = Path.Combine(workspace.DataDirectory, "not-a-directory");
        File.WriteAllText(fileInTheWay, "x");

        var exitCode = Regenerate(workspace, Path.Combine(fileInTheWay, "Dashboard"));

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to regenerate the Dashboard: ", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    // The workspace is root/{base, logs-in, data}; the database lives in data, the default log output in base/Logs.
    [Theory]
    [InlineData("{root}")]
    [InlineData("{root-upper-case}")]
    public void Regenerate_OutputDirectoryContainsTheBaseDirectory_RefusesAndDeletesNothing(string requestedOutputDirectory)
    {
        ArgumentNullException.ThrowIfNull(requestedOutputDirectory);
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var root = Path.GetDirectoryName(workspace.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!;
        var outputDirectory = requestedOutputDirectory
            .Replace("{root}", root, StringComparison.Ordinal)
            .Replace("{root-upper-case}", root.ToUpperInvariant(), StringComparison.Ordinal);
        var logSourceFile = workspace.WriteLogFile("u_ex260501_x_1.log", "keep me");

        var exitCode = Regenerate(workspace, outputDirectory);

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
        workspace.WriteAppSettings();

        var exitCode = Regenerate(workspace, workspace.DataDirectory);

        Assert.Equal(1, exitCode);
        Assert.Contains("it contains the aggregate database '", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(workspace.DatabasePath()));
    }

    [Theory]
    [InlineData("{base}Logs")]
    [InlineData("{base}./Logs/")]
    [InlineData("{base}Logs/nested/..")]
    public void Regenerate_OutputDirectoryIsTheLogOutputDirectory_RefusesAndKeepsItsContents(string requestedOutputDirectory)
    {
        ArgumentNullException.ThrowIfNull(requestedOutputDirectory);
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var outputDirectory = requestedOutputDirectory.Replace("{base}", workspace.BaseDirectory, StringComparison.Ordinal);
        var logFile = Path.Combine(Directory.CreateDirectory(Path.Combine(workspace.BaseDirectory, "Logs")).FullName, "iislogparser.log");
        File.WriteAllText(logFile, "keep me");

        var exitCode = Regenerate(workspace, outputDirectory);

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
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["LogOutputDirectory"] = logDirectory });
        var logFile = Path.Combine(Directory.CreateDirectory(logDirectory).FullName, "iislogparser.log");
        File.WriteAllText(logFile, "keep me");

        var exitCode = Regenerate(workspace, outputDirectory);

        Assert.Equal(1, exitCode);
        Assert.Contains("it contains the log output directory '", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(logFile));
    }

    [Fact]
    public void Regenerate_OutputDirectoryIsTheLogSourceDirectory_RefusesAndKeepsItsContents()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var logSourceFile = workspace.WriteLogFile("u_ex260501_x_1.log", "keep me");

        var exitCode = Regenerate(workspace, workspace.LogSourceDirectory, logSourceDirectory: workspace.LogSourceDirectory);

        Assert.Equal(1, exitCode);
        Assert.Contains("it resolves to the log source directory, which this run would delete recursively.", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(logSourceFile));
    }

    [Fact]
    public void Regenerate_OutputDirectoryContainsTheLogSourceDirectory_RefusesAndKeepsItsContents()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var archiveDirectory = workspace.CreateDirectoryUnderRoot("archive");
        var nestedLogSourceDirectory = Directory.CreateDirectory(Path.Combine(archiveDirectory, "iis-logs")).FullName;
        var logSourceFile = Path.Combine(nestedLogSourceDirectory, "u_ex260501_x_1.log");
        File.WriteAllText(logSourceFile, "keep me");

        var exitCode = Regenerate(workspace, archiveDirectory, logSourceDirectory: nestedLogSourceDirectory);

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
        Assert.False(Directory.Exists(DefaultOutputDirectory(workspace)));
    }

    [Fact]
    public void Regenerate_OutputDirectoryOnlySharesANamePrefixWithTheBaseDirectory_IsNotMistakenForIt()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var root = Path.GetDirectoryName(workspace.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!;
        var outputDirectory = Path.Combine(root, "base-dashboard");

        var exitCode = Regenerate(workspace, outputDirectory);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, workspace.Error.ToString());
        Assert.True(File.Exists(Path.Combine(outputDirectory, "index.html")));
    }

    [Fact]
    public void Regenerate_OutputDirectoryIsInsideAProtectedDirectory_IsAllowed()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var outputDirectory = Path.Combine(workspace.DataDirectory, "dashboard");

        var exitCode = Regenerate(workspace, outputDirectory, logSourceDirectory: workspace.LogSourceDirectory);

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

    private static string DefaultOutputDirectory(CliTestWorkspace workspace)
    {
        return Path.Combine(workspace.DataDirectory, "Dashboard");
    }

    private static int Regenerate(CliTestWorkspace workspace, string? outputDirectory = null, string? logSourceDirectory = null)
    {
        var configuration = AppConfigurationFactory.Build(workspace.BaseDirectory, "Production");
        using var connection = OpenDatabase(workspace);
        return new DashboardRegenerator(workspace.Environment())
            .Regenerate(connection, configuration, Path.GetFullPath(outputDirectory ?? DefaultOutputDirectory(workspace)), Year, logSourceDirectory);
    }
}
