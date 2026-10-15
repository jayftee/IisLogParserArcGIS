using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Reports;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests;

public class RegenerationRunTests
{
    [Fact]
    public void Run_AgainstAPopulatedDatabase_CreatesTheOutputDirectory()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path, 2026);

        Assert.True(Directory.Exists(outputDirectory.Path));
    }

    [Fact]
    public void Run_ReturnsBoundaries_ResolvedFromTheGivenTimeProvider()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        var boundaries = RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path, 2026);

        Assert.Equal(new DateOnly(2026, 6, 15), boundaries.Today);
        Assert.Equal(new DateOnly(2026, 1, 1), boundaries.YearStart);
    }

    [Fact]
    public void Run_WhenRunTwice_ReplacesPriorOutputInsteadOfMerging()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path, 2026);
        var staleFilePath = Path.Combine(outputDirectory.Path, "stale-from-prior-run.html");
        File.WriteAllText(staleFilePath, "stale");

        RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path, 2026);

        Assert.True(Directory.Exists(outputDirectory.Path));
        Assert.False(File.Exists(staleFilePath));
    }

    [Fact]
    public void Run_WithNullConnection_ThrowsArgumentNullException()
    {
        using var outputDirectory = new TempDirectoryPath();
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => RegenerationRun.Run(null!, settings, timeProvider, outputDirectory.Path, 2026));
    }

    [Fact]
    public void Run_WithNullSettings_ThrowsArgumentNullException()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => RegenerationRun.Run(connection, null!, timeProvider, outputDirectory.Path, 2026));
    }

    [Fact]
    public void Run_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();

        Assert.Throws<ArgumentNullException>(() => RegenerationRun.Run(connection, settings, null!, outputDirectory.Path, 2026));
    }

    [Fact]
    public void Run_WithNullOutputDirectory_ThrowsArgumentNullException()
    {
        using var databasePath = new TempDatabasePath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => RegenerationRun.Run(connection, settings, timeProvider, null!, 2026));
    }

    [Fact]
    public void Run_WithOutputDirectoryResolvingToADriveRoot_ThrowsArgumentException()
    {
        using var databasePath = new TempDatabasePath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var driveRoot = Path.GetPathRoot(Path.GetTempPath())!;

        Assert.Throws<ArgumentException>(() => RegenerationRun.Run(connection, settings, timeProvider, driveRoot, 2026));
    }

    [Fact]
    public void Run_WritesTheDashboardMarkerFile_IntoTheOutputDirectory()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path, 2026);

        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, RegenerationRun.MarkerFileName)));
    }

    [Fact]
    public void Run_WithAnExistingEmptyOutputDirectory_Proceeds()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        Directory.CreateDirectory(outputDirectory.Path);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path, 2026);

        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "index.html")));
    }

    [Fact]
    public void Run_WithANonEmptyOutputDirectoryThatLacksTheMarker_ThrowsAndDeletesNothing()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        Directory.CreateDirectory(outputDirectory.Path);
        var precious = Path.Combine(outputDirectory.Path, "precious.txt");
        File.WriteAllText(precious, "keep me");

        var exception = Assert.Throws<ArgumentException>(
            () => RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path, 2026));

        Assert.Contains(RegenerationRun.MarkerFileName, exception.Message, StringComparison.Ordinal);
        Assert.Contains(outputDirectory.Path, exception.Message, StringComparison.Ordinal);
        Assert.Equal("keep me", File.ReadAllText(precious));
    }

    [Fact]
    public void Run_WithANonEmptyOutputDirectoryThatHasTheMarker_ReplacesItsContents()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        Directory.CreateDirectory(outputDirectory.Path);
        File.WriteAllText(Path.Combine(outputDirectory.Path, RegenerationRun.MarkerFileName), string.Empty);
        var stale = Path.Combine(outputDirectory.Path, "stale.html");
        File.WriteAllText(stale, "stale");

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path, 2026);

        Assert.False(File.Exists(stale));
        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, RegenerationRun.MarkerFileName)));
    }

    [Fact]
    public void Run_ForAPastYear_ReturnsBoundariesThatCoverThatWholeYear()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);

        var boundaries = RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path, 2025);

        Assert.Equal(new DateOnly(2025, 1, 1), boundaries.YearStart);
        Assert.Equal(new DateOnly(2025, 12, 31), boundaries.Today);
    }

    [Fact]
    public void Run_ForAYearAfterTheCurrentOne_ThrowsAndLeavesTheOutputDirectoryUntouched()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), outputDirectory.Path, 2027));

        Assert.False(Directory.Exists(outputDirectory.Path));
    }

    [Fact]
    public void Run_TwoRunsIntoDifferentOutputDirectories_NeverTouchEachOthersDirectory()
    {
        using var databasePath = new TempDatabasePath();
        using var firstDirectory = new TempDirectoryPath();
        using var secondDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);

        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), firstDirectory.Path, 2025);
        var firstIndexWrittenAt = File.GetLastWriteTimeUtc(Path.Combine(firstDirectory.Path, "index.html"));
        RegenerationRun.Run(connection, CreateSettings(), CreateTimeProvider(), secondDirectory.Path, 2026);

        Assert.Equal(firstIndexWrittenAt, File.GetLastWriteTimeUtc(Path.Combine(firstDirectory.Path, "index.html")));
        Assert.True(File.Exists(Path.Combine(secondDirectory.Path, "index.html")));
    }

    private static FakeTimeProvider CreateTimeProvider()
    {
        return new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
    }

    private static SqliteConnection OpenSchemaBackedConnection(string databasePath)
    {
        var connection = SqliteConnectionFactory.Open(databasePath);
        AggregateDatabaseSchema.EnsureCreated(connection);
        return connection;
    }

    private static ReportsSettings CreateSettings()
    {
        return new ReportsSettings
        {
            LocalTimeZone = "UTC",
            IncludedRoots = ["arcgis"],
            PortalBaseUrl = "portal.example.com",
        };
    }

    private sealed class TempDatabasePath : IDisposable
    {
        public TempDatabasePath()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
        }

        public string Path { get; }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
