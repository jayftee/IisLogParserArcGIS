using IisLogParserArcGIS.FileDiscovery;

namespace IisLogParserArcGIS.Tests.FileDiscovery;

public class LogFileLocatorTests
{
    [Fact]
    public void Discover_WithZeroOffset_FindsFilesForTargetDateOnly_IgnoringSiteIdSegment()
    {
        using var sourceDirectory = new TempDirectory(
            "u_ex260501_x_10359.log",
            "u_ex260501_x_10360.log",
            "u_ex260502_x_10359.log");
        var locator = new LogFileLocator();

        var found = locator.Discover(sourceDirectory.Path, new DateOnly(2026, 5, 1), TimeSpan.Zero);

        Assert.Equal(2, found.Count);
        Assert.All(found, path => Assert.Contains("260501", path, StringComparison.Ordinal));
    }

    [Fact]
    public void Discover_TreatsEverythingAfterTheUtcDateAsOneOpaqueWildcard()
    {
        using var sourceDirectory = new TempDirectory(
            "u_ex260501.log",
            "u_ex260501_x.log",
            "u_ex260501_x_10359.log",
            "u_ex260502_x_10359.log");
        var locator = new LogFileLocator();

        var found = locator.Discover(sourceDirectory.Path, new DateOnly(2026, 5, 1), TimeSpan.Zero);

        Assert.Equal(3, found.Count);
        Assert.All(found, path => Assert.Contains("260501", path, StringComparison.Ordinal));
    }

    [Fact]
    public void Discover_WithNegativeOffset_AlsoIncludesDayAfterUtcFile()
    {
        using var sourceDirectory = new TempDirectory(
            "u_ex260501_x_10359.log",
            "u_ex260502_x_10359.log",
            "u_ex260503_x_10359.log");
        var locator = new LogFileLocator();

        var found = locator.Discover(sourceDirectory.Path, new DateOnly(2026, 5, 1), TimeSpan.FromHours(-5));

        Assert.Equal(2, found.Count);
        Assert.Contains(found, path => path.Contains("260501", StringComparison.Ordinal));
        Assert.Contains(found, path => path.Contains("260502", StringComparison.Ordinal));
    }

    [Fact]
    public void Discover_WithPositiveOffset_AlsoIncludesDayBeforeUtcFile()
    {
        using var sourceDirectory = new TempDirectory(
            "u_ex260430_x_10359.log",
            "u_ex260501_x_10359.log",
            "u_ex260502_x_10359.log");
        var locator = new LogFileLocator();

        var found = locator.Discover(sourceDirectory.Path, new DateOnly(2026, 5, 1), TimeSpan.FromHours(9));

        Assert.Equal(2, found.Count);
        Assert.Contains(found, path => path.Contains("260430", StringComparison.Ordinal));
        Assert.Contains(found, path => path.Contains("260501", StringComparison.Ordinal));
    }

    [Fact]
    public void Discover_WithNegativeOffset_MissingDayAfterFile_ThrowsNoLogFilesFoundException()
    {
        using var sourceDirectory = new TempDirectory("u_ex260501_x_10359.log");
        var locator = new LogFileLocator();

        var ex = Assert.Throws<NoLogFilesFoundException>(
            () => locator.Discover(sourceDirectory.Path, new DateOnly(2026, 5, 1), TimeSpan.FromHours(-5)));

        Assert.Contains("260502", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Discover_WithNoMatchingFiles_ThrowsNoLogFilesFoundException()
    {
        using var sourceDirectory = new TempDirectory("u_ex260601_x_10359.log");
        var locator = new LogFileLocator();

        var ex = Assert.Throws<NoLogFilesFoundException>(
            () => locator.Discover(sourceDirectory.Path, new DateOnly(2026, 5, 1), TimeSpan.Zero));

        Assert.Contains("No log files found", ex.Message, StringComparison.Ordinal);
        Assert.Contains("260501", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Discover_WithNonExistentDirectory_ThrowsNoLogFilesFoundException()
    {
        var locator = new LogFileLocator();
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Throws<NoLogFilesFoundException>(
            () => locator.Discover(missingDirectory, new DateOnly(2026, 5, 1), TimeSpan.Zero));
    }

    [Fact]
    public void Discover_WithNullSourceDirectory_ThrowsArgumentNullException()
    {
        var locator = new LogFileLocator();

        Assert.Throws<ArgumentNullException>(() => locator.Discover(null!, new DateOnly(2026, 5, 1), TimeSpan.Zero));
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory(params string[] fileNames)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);

            foreach (var fileName in fileNames)
            {
                File.WriteAllText(System.IO.Path.Combine(Path, fileName), string.Empty);
            }
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
