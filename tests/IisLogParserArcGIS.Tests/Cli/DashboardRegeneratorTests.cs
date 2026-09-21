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
        Assert.Contains("drive root", workspace.Error.ToString(), StringComparison.Ordinal);
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

    private static SqliteConnection OpenDatabase(CliTestWorkspace workspace)
    {
        var connection = SqliteConnectionFactory.Open(workspace.DatabasePath());
        AggregateDatabaseSchema.EnsureCreated(connection);
        return connection;
    }

    private static int Regenerate(CliTestWorkspace workspace)
    {
        var configuration = AppConfigurationFactory.Build(workspace.BaseDirectory, "Production");
        using var connection = OpenDatabase(workspace);

        return new DashboardRegenerator(workspace.Environment()).Regenerate(connection, configuration);
    }
}
