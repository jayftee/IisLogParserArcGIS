using IisLogParserArcGIS.Parsing;

namespace IisLogParserArcGIS.Tests.Parsing;

public class LogCorpusParserTests
{
    private const string StandardHeader = "#Fields: date time cs-uri-stem cs(User-Agent) cs(Referer) sc-status time-taken X-Forwarded-For";

    [Fact]
    public void Parse_WithMultipleValidFiles_AggregatesRequestsAndCounts()
    {
        using var directory = new TempDirectory();
        var fileA = directory.WriteFile("u_ex260501_x_1.log", StandardHeader, "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -");
        var fileB = directory.WriteFile("u_ex260502_x_1.log", StandardHeader, "2026-05-02 01:00:00 /b Mozilla/5.0 - 200 20 -");
        var parser = new LogCorpusParser();

        var result = parser.Parse([fileA, fileB], TimeSpan.Zero);

        Assert.Equal(2, result.Requests.Count);
        Assert.Equal(2, result.Counts.Total);
        Assert.Equal(2, result.Counts.Valid);
        Assert.Equal(0, result.Counts.Invalid);
        Assert.Equal(0, result.SkippedFileCount);
    }

    [Fact]
    public void Parse_WithOneSkippedFile_ExcludesItsRequestsAndCountsTheSkip()
    {
        using var directory = new TempDirectory();
        var goodFile = directory.WriteFile("u_ex260501_x_1.log", StandardHeader, "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -");
        var badFile = directory.WriteFile(
            "u_ex260501_x_2.log",
            "#Fields: date time cs-uri-stem cs(User-Agent) sc-status time-taken",
            "2026-05-01 12:00:00 /a Mozilla/5.0 200 10");
        var parser = new LogCorpusParser();

        var result = parser.Parse([goodFile, badFile], TimeSpan.Zero);

        Assert.Single(result.Requests);
        Assert.Equal(1, result.SkippedFileCount);
    }

    [Fact]
    public void Parse_WithOneUnreadableFile_SkipsItAndStillParsesTheOthers()
    {
        using var directory = new TempDirectory();
        var goodFile = directory.WriteFile("u_ex260501_x_1.log", StandardHeader, "2026-05-01 12:00:00 /a Mozilla/5.0 - 200 10 -");
        var missingFile = System.IO.Path.Combine(directory.Path, "u_ex260501_x_2.log.missing");
        var parser = new LogCorpusParser();

        var result = parser.Parse([goodFile, missingFile], TimeSpan.Zero);

        Assert.Single(result.Requests);
        Assert.Equal(1, result.SkippedFileCount);
    }

    [Fact]
    public void Parse_WithNoFiles_ReturnsEmptyResult()
    {
        var parser = new LogCorpusParser();

        var result = parser.Parse([], TimeSpan.Zero);

        Assert.Empty(result.Requests);
        Assert.Equal(0, result.Counts.Total);
        Assert.Equal(0, result.SkippedFileCount);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string WriteFile(string fileName, params string[] lines)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            File.WriteAllLines(filePath, lines);
            return filePath;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
