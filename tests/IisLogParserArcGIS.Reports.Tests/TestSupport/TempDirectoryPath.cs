namespace IisLogParserArcGIS.Reports.Tests.TestSupport;

/// <summary>
/// A directory at a unique temporary path, deleted (recursively) on disposal - the output directory passed to
/// <c>RegenerationRun.Run</c> for a single test run.
/// </summary>
internal sealed class TempDirectoryPath : IDisposable
{
    public TempDirectoryPath()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
