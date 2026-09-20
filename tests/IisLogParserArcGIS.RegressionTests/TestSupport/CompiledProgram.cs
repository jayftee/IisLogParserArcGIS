using System.Globalization;

namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// Locates and runs the shipped console executable and the real-world <c>2026/</c> regression corpus, both
/// resolved relative to the repository root rather than assumed at a fixed path.
/// </summary>
internal static class CompiledProgram
{
    private const string ExecutableFileName = "IisLogParserArcGIS.exe";
    private const string SolutionFileName = "IisLogParserArcGIS.slnx";
    private const string CorpusDirectoryName = "2026";
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly TimeSpan _runTimeout = TimeSpan.FromSeconds(120);
    private static readonly Lazy<string> _repositoryRoot = new(FindRepositoryRoot);
    private static readonly Lazy<string> _executablePath = new(LocateExecutable);
    private static readonly Lazy<string> _corpusDirectoryPath = new(LocateCorpusDirectory);

    /// <summary>
    /// Gets the full path of the real-world <c>2026/</c> log corpus directory.
    /// </summary>
    public static string CorpusDirectory => _corpusDirectoryPath.Value;

    /// <summary>
    /// Runs the shipped executable's <c>harvest</c> verb against the full <see cref="CorpusDirectory"/>, for
    /// <paramref name="targetLocalDate"/>, writing to <paramref name="outputDatabasePath"/> - the same arguments
    /// an operator would pass to harvest one date. Deliberately <c>harvest</c>, not <c>harvest-regenerate</c>:
    /// this suite only ever asserts on the process's exit code/output and the aggregate database, never the
    /// Dashboard, so there is no reason to pay for a Regeneration Run on every invocation.
    /// </summary>
    /// <param name="targetLocalDate">The target local date to process.</param>
    /// <param name="outputDatabasePath">The output SQLite database path.</param>
    /// <returns>The process's external, observable outcome.</returns>
    public static Task<ExecutableRunResult> RunAsync(DateOnly targetLocalDate, string outputDatabasePath)
    {
        ArgumentNullException.ThrowIfNull(outputDatabasePath);

        string[] arguments =
        [
            "harvest",
            CorpusDirectory,
            targetLocalDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            outputDatabasePath,
        ];

        return ProcessRunner.RunAsync(_executablePath.Value, arguments, _runTimeout);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, SolutionFileName)) is false)
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException($"Could not locate the repository root ({SolutionFileName}) above '{AppContext.BaseDirectory}'.");
    }

    private static string LocateExecutable()
    {
        var consoleProjectBinDirectory = Path.Combine(_repositoryRoot.Value, "src", "IisLogParserArcGIS", "bin");

        if (Directory.Exists(consoleProjectBinDirectory) is false)
        {
            throw new FileNotFoundException(
                $"The console host has not been built yet: '{consoleProjectBinDirectory}' does not exist. " +
                "Build the solution before running the regression suite.");
        }

        var ownOutputDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var configurationAndTfm = Path.Combine(ownOutputDirectory.Parent!.Name, ownOutputDirectory.Name);
        var expectedPath = Path.Combine(consoleProjectBinDirectory, configurationAndTfm, ExecutableFileName);

        if (File.Exists(expectedPath))
        {
            return expectedPath;
        }

        var candidates = Directory.GetFiles(consoleProjectBinDirectory, ExecutableFileName, SearchOption.AllDirectories);

        if (candidates.Length == 0)
        {
            throw new FileNotFoundException(
                $"No compiled '{ExecutableFileName}' found under '{consoleProjectBinDirectory}'. " +
                "Build the solution before running the regression suite.");
        }

        return candidates.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    private static string LocateCorpusDirectory()
    {
        var corpusDirectory = Path.Combine(_repositoryRoot.Value, CorpusDirectoryName);

        if (Directory.Exists(corpusDirectory) is false)
        {
            throw new DirectoryNotFoundException(
                $"The real-world (anonymized) regression corpus was not found at '{corpusDirectory}'. It is checked into the " +
                "repository at the repository root - check out or restore the 2026/ folder before running this suite (see docs/adr/0009).");
        }

        return corpusDirectory;
    }
}
