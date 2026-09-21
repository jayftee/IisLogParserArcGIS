using IisLogParserArcGIS.Cli;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Tests.Cli;

public class HarvestRunnerTests
{
    private const string TargetDate = "2026-05-01";
    private const string StandardHeader = "#Fields: date time cs-uri-stem cs(User-Agent) cs(Referer) sc-status time-taken X-Forwarded-For";

    [Fact]
    public void Run_Harvest_PersistsTheDaysAggregatesAndDoesNotGenerateTheDashboard()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, workspace.Error.ToString());
        Assert.Equal(2, TotalByUriHits(databasePath));
        Assert.False(Directory.Exists(Path.Combine(workspace.BaseDirectory, "Dashboard")));
    }

    [Fact]
    public void Run_WithComputeByRefererAndUriDisabled_LeavesThatTableEmptyButPopulatesTheOthers()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile(new Dictionary<string, object?> { ["ComputeByRefererAndUri"] = false });
        var databasePath = workspace.DatabasePath();

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, ByRefererAndUriRowCount(databasePath));
        Assert.Equal(2, TotalByUriHits(databasePath));
    }

    [Fact]
    public void Run_WithoutTheComputeByRefererAndUriKey_BehavesAsDisabled()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(0, exitCode);
        Assert.Equal(0, ByRefererAndUriRowCount(databasePath));
    }

    [Fact]
    public void Run_WithComputeByRefererAndUriEnabled_PersistsTheByRefererAndUriRows()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile(new Dictionary<string, object?> { ["ComputeByRefererAndUri"] = true });
        var databasePath = workspace.DatabasePath();

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(0, exitCode);
        Assert.Equal(2, ByRefererAndUriRowCount(databasePath));
    }

    [Fact]
    public void Run_TurningComputeByRefererAndUriOffAfterItWasOn_LeavesTheEarlierRowsInPlace()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile(new Dictionary<string, object?> { ["ComputeByRefererAndUri"] = true });
        var databasePath = workspace.DatabasePath();
        Assert.Equal(0, Harvest(workspace, databasePath));

        workspace.WriteAppSettings(new Dictionary<string, object?> { ["ComputeByRefererAndUri"] = false });
        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(0, exitCode);
        Assert.Equal(2, ByRefererAndUriRowCount(databasePath));
    }

    [Fact]
    public void Run_HarvestRegenerate_PersistsTheDaysAggregatesAndGeneratesTheDashboard()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();

        var exitCode = HarvestAndRegenerate(workspace, databasePath);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, workspace.Error.ToString());
        Assert.Equal(2, TotalByUriHits(databasePath));
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "Dashboard", "index.html")));
    }

    [Fact]
    public void Run_HarvestRegenerate_WhenTheDashboardOutputIsRefused_ReturnsOneButKeepsTheHarvestedRows()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile(new Dictionary<string, object?> { ["OutputDirectory"] = "." });
        var databasePath = workspace.DatabasePath();

        var exitCode = HarvestAndRegenerate(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to regenerate the Dashboard: Refusing to use", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, TotalByUriHits(databasePath));
        Assert.True(File.Exists(Path.Combine(workspace.BaseDirectory, "appsettings.json")));
    }

    [Fact]
    public void Run_HarvestRegenerate_WhenTheDashboardOutputIsTheLogSourceDirectory_ReturnsOneAndKeepsTheIisLogs()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(new Dictionary<string, object?> { ["OutputDirectory"] = workspace.LogSourceDirectory });
        var logFile = workspace.WriteLogFile(
            "u_ex260501_x_1.log",
            StandardHeader,
            "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -");
        var databasePath = workspace.DatabasePath();

        var exitCode = HarvestAndRegenerate(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.Contains("it resolves to the log source directory", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(logFile));
        Assert.Equal(1, TotalByUriHits(databasePath));
    }

    [Fact]
    public void Run_TheSameDateTwice_ReplacesTheDaysRowsInsteadOfDoublingThem()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();

        var firstExitCode = Harvest(workspace, databasePath);
        var secondExitCode = Harvest(workspace, databasePath);

        Assert.Equal(0, firstExitCode);
        Assert.Equal(0, secondExitCode);
        Assert.Equal(2, TotalByUriHits(databasePath));
    }

    [Fact]
    public void Run_OneOfTheDaysFilesIsSkipped_RefusesToReplaceTheDayAndKeepsTheExistingRows()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();
        Assert.Equal(0, Harvest(workspace, databasePath));
        WriteFileWithMissingRequiredFields(workspace, "u_ex260501_x_2.log");

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Refusing to replace 2026-05-01: 1 of 2 log file(s) were skipped", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, TotalByUriHits(databasePath));
    }

    [Fact]
    public void Run_EveryFileOfTheDayIsSkipped_RefusesToReplaceTheDayInsteadOfWipingIt()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();
        Assert.Equal(0, Harvest(workspace, databasePath));
        WriteFileWithMissingRequiredFields(workspace, "u_ex260501_x_1.log");

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Refusing to replace 2026-05-01: 1 of 1 log file(s) were skipped", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, TotalByUriHits(databasePath));
    }

    [Fact]
    public void Run_HarvestRegenerate_WhenAFileIsSkipped_ReturnsOneWithoutGeneratingTheDashboard()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        WriteFileWithMissingRequiredFields(workspace, "u_ex260501_x_2.log");

        var exitCode = HarvestAndRegenerate(workspace, workspace.DatabasePath());

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Refusing to replace 2026-05-01", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(workspace.BaseDirectory, "Dashboard")));
    }

    [Fact]
    public void Run_AFileCannotBeRead_RefusesToReplaceTheDay()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();
        var lockedFile = workspace.WriteLogFile("u_ex260501_x_2.log", StandardHeader, "2026-05-01 12:00:00 /c Mozilla/5.0 - 200 10 -");
        using var exclusiveHandle = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Refusing to replace 2026-05-01: 1 of 2 log file(s) were skipped", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_MalformedDate_ReturnsOneWithTheValidationMessageAndTouchesNothing()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();

        var exitCode = new HarvestRunner(workspace.Environment()).Harvest(Arguments(workspace.LogSourceDirectory, "not-a-date", databasePath));

        Assert.Equal(1, exitCode);
        Assert.Equal($"The target local date 'not-a-date' is not valid. Expected format: yyyy-MM-dd.{Environment.NewLine}", workspace.Error.ToString());
        Assert.False(File.Exists(databasePath));
        Assert.False(Directory.Exists(Path.Combine(workspace.BaseDirectory, "Logs")));
    }

    [Fact]
    public void Run_EmptyLogSourceDirectory_ReturnsOneWithTheValidationMessage()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();

        var exitCode = new HarvestRunner(workspace.Environment()).Harvest(Arguments(string.Empty, TargetDate, workspace.DatabasePath()));

        Assert.Equal(1, exitCode);
        Assert.Equal($"The log source directory must not be empty.{Environment.NewLine}", workspace.Error.ToString());
    }

    [Fact]
    public void Run_NoLogFilesForTheTargetDate_ReturnsOneWithTheNoLogFilesMessage()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings();
        var databasePath = workspace.DatabasePath();

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            $"No log files found in '{workspace.LogSourceDirectory}' for UTC date(s) 260501 (local date 2026-05-01).{Environment.NewLine}",
            workspace.Error.ToString());
    }

    [Fact]
    public void Run_InvalidConfiguredLocalTimeZone_ReturnsOneWithTheTimeZoneMessageAndDoesNotCreateTheDatabase()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile(new Dictionary<string, object?> { ["LocalTimeZone"] = "Not/AZone" });
        var databasePath = workspace.DatabasePath();

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Invalid configured local time zone 'Not/AZone': ", workspace.Error.ToString(), StringComparison.Ordinal);
        Assert.False(File.Exists(databasePath));
    }

    [Fact]
    public void Run_TheDatabaseCannotBeWrittenDuringTheHarvest_ReturnsOneWithTheHarvestFailureMessage()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = workspace.DatabasePath();
        using (var connection = SqliteConnectionFactory.Open(databasePath))
        {
            AggregateDatabaseSchema.EnsureCreated(connection);
            SqliteConnection.ClearPool(connection);
        }

        File.SetAttributes(databasePath, FileAttributes.ReadOnly);
        try
        {
            var exitCode = Harvest(workspace, databasePath);

            Assert.Equal(1, exitCode);
            Assert.StartsWith("Failed while harvesting the log files into the output database: ", workspace.Error.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("initialize", workspace.Error.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.SetAttributes(databasePath, FileAttributes.Normal);
        }
    }

    [Fact]
    public void Run_OutputDatabaseInAMissingDirectory_ReturnsOneWithTheInitializationMessage()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        var databasePath = Path.Combine(workspace.DataDirectory, "no-such-directory", "aggregates.sqlite");

        var exitCode = Harvest(workspace, databasePath);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to initialize configuration, logging, or the output database: ", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_MissingAppSettings_ReturnsOneWithTheInitializationMessage()
    {
        using var workspace = new CliTestWorkspace();
        workspace.WriteLogFile("u_ex260501_x_1.log", StandardHeader, "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -");

        var exitCode = Harvest(workspace, workspace.DatabasePath());

        Assert.Equal(1, exitCode);
        Assert.StartsWith("Failed to initialize configuration, logging, or the output database: ", workspace.Error.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Development", "DevLogs")]
    [InlineData("Production", "Logs")]
    public void Run_UsesTheAppSettingsOverlayOfTheEnvironmentName(string environmentName, string expectedLogDirectory)
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();
        workspace.WriteOverlay("Development", new Dictionary<string, object?> { ["LogOutputDirectory"] = "DevLogs" });

        var exitCode = new HarvestRunner(workspace.Environment(environmentName)).Harvest(
            Arguments(workspace.LogSourceDirectory, TargetDate, workspace.DatabasePath()));

        Assert.Equal(0, exitCode);
        var otherLogDirectory = expectedLogDirectory == "Logs" ? "DevLogs" : "Logs";
        Assert.True(Directory.Exists(Path.Combine(workspace.BaseDirectory, expectedLogDirectory)));
        Assert.False(Directory.Exists(Path.Combine(workspace.BaseDirectory, otherLogDirectory)));
    }

    [Fact]
    public void Run_WritesTheRunLogUnderTheConfiguredLogDirectoryOfTheBaseDirectory()
    {
        using var workspace = PrepareWorkspaceWithOneLogFile();

        var exitCode = Harvest(workspace, workspace.DatabasePath());

        Assert.Equal(0, exitCode);
        var logFile = Directory.GetFiles(Path.Combine(workspace.BaseDirectory, "Logs"), "iislogparser*.log").Single();
        Assert.NotEqual(string.Empty, File.ReadAllText(logFile));
    }

    private static CliTestWorkspace PrepareWorkspaceWithOneLogFile(IReadOnlyDictionary<string, object?>? appSettingsOverrides = null)
    {
        var workspace = new CliTestWorkspace();
        workspace.WriteAppSettings(appSettingsOverrides);
        workspace.WriteLogFile(
            "u_ex260501_x_1.log",
            StandardHeader,
            "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -",
            "2026-05-01 13:00:00 /b Mozilla/5.0 - 200 20 -");
        return workspace;
    }

    private static void WriteFileWithMissingRequiredFields(CliTestWorkspace workspace, string fileName)
    {
        workspace.WriteLogFile(
            fileName,
            "#Fields: date time cs-uri-stem cs(User-Agent) sc-status time-taken",
            "2026-05-01 12:00:00 /a Mozilla/5.0 200 10");
    }

    private static HarvestArguments Arguments(string logSourceDirectory, string targetLocalDate, string outputDatabasePath)
    {
        return new HarvestArguments
        {
            LogSourceDirectory = logSourceDirectory,
            TargetLocalDate = targetLocalDate,
            OutputDatabasePath = outputDatabasePath,
        };
    }

    private static int Harvest(CliTestWorkspace workspace, string databasePath)
    {
        return new HarvestRunner(workspace.Environment()).Harvest(Arguments(workspace.LogSourceDirectory, TargetDate, databasePath));
    }

    private static int HarvestAndRegenerate(CliTestWorkspace workspace, string databasePath)
    {
        return new HarvestRunner(workspace.Environment()).HarvestAndRegenerate(Arguments(workspace.LogSourceDirectory, TargetDate, databasePath));
    }

    private static int ByRefererAndUriRowCount(string databasePath)
    {
        using var connection = SqliteConnectionFactory.Open(databasePath);
        return new ByRefererAndUriRepository().GetByLocalDate(connection, new DateOnly(2026, 5, 1)).Count();
    }

    private static int TotalByUriHits(string databasePath)
    {
        using var connection = SqliteConnectionFactory.Open(databasePath);
        return new ByUriRepository().GetByLocalDate(connection, new DateOnly(2026, 5, 1)).Sum(row => row.Hits);
    }
}
