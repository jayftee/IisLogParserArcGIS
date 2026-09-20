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

        RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path);

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

        var boundaries = RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path);

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

        RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path);
        var staleFilePath = Path.Combine(outputDirectory.Path, "stale-from-prior-run.html");
        File.WriteAllText(staleFilePath, "stale");

        RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path);

        Assert.True(Directory.Exists(outputDirectory.Path));
        Assert.False(File.Exists(staleFilePath));
    }

    [Fact]
    public void Run_WithNullConnection_ThrowsArgumentNullException()
    {
        using var outputDirectory = new TempDirectoryPath();
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => RegenerationRun.Run(null!, settings, timeProvider, outputDirectory.Path));
    }

    [Fact]
    public void Run_WithNullSettings_ThrowsArgumentNullException()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => RegenerationRun.Run(connection, null!, timeProvider, outputDirectory.Path));
    }

    [Fact]
    public void Run_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        using var databasePath = new TempDatabasePath();
        using var outputDirectory = new TempDirectoryPath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();

        Assert.Throws<ArgumentNullException>(() => RegenerationRun.Run(connection, settings, null!, outputDirectory.Path));
    }

    [Fact]
    public void Run_WithNullOutputDirectory_ThrowsArgumentNullException()
    {
        using var databasePath = new TempDatabasePath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => RegenerationRun.Run(connection, settings, timeProvider, null!));
    }

    [Fact]
    public void Run_WithOutputDirectoryResolvingToADriveRoot_ThrowsArgumentException()
    {
        using var databasePath = new TempDatabasePath();
        using var connection = OpenSchemaBackedConnection(databasePath.Path);
        var settings = CreateSettings();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var driveRoot = Path.GetPathRoot(Path.GetTempPath())!;

        Assert.Throws<ArgumentException>(() => RegenerationRun.Run(connection, settings, timeProvider, driveRoot));
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
            OutputDirectory = "Dashboard",
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
