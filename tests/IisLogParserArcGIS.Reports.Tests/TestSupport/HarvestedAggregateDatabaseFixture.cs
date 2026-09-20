using System.Globalization;
using System.Text.RegularExpressions;
using IisLogParserArcGIS.RegressionTests.TestSupport;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports.Tests.TestSupport;

/// <summary>
/// Builds one shared, multi-day, multi-entity aggregate SQLite database from the real, checked-in <c>2026/</c>
/// corpus - the standard fixture strategy for this project's tests, in place of hand-typed fixture rows. It
/// replays the <c>IisLogParserArcGIS.RegressionTests</c> project's own <see cref="CompiledProgram"/> tooling
/// (the shipped executable run as a black box) once per corpus date, into the same output database file.
/// Because a Daily Batch replace only ever touches its own <c>local_date</c>'s rows (see
/// <c>DailyAggregateReplacer</c>), accumulating one date at a time into a single file is safe and yields the
/// corpus's real Roots (including the <c>proxy</c> Root) and a realistic spread of ArcGIS Server services and
/// Portal Items.
/// </summary>
/// <remarks>
/// This is expensive - one process invocation per corpus date - so it is built once per test collection rather
/// than once per test. Register a test class into <see cref="HarvestedAggregateDatabaseCollection"/> and take
/// this type as a constructor parameter to reuse the already-built database. Every test in that collection
/// shares <see cref="DatabasePath"/> read-only; a test needing edge-case rows the corpus doesn't naturally
/// contain (an exact ranking tie, an entity present for only part of the date range) should mutate a
/// <see cref="WorkingDatabaseCopy"/> instead of this shared file. Building it runs the real shipped executable's
/// <c>harvest</c> verb - never <c>harvest-regenerate</c>, since this fixture only ever needs the aggregate
/// database, not a Dashboard - which, as a side effect, still writes to its own rolling log file next to that
/// executable; a cross-process named semaphore in <c>ProcessRunner</c> serializes this against the
/// <c>IisLogParserArcGIS.RegressionTests</c> suite's own invocations of the same executable, so the two test
/// projects can safely run at the same time.
/// </remarks>
public sealed class HarvestedAggregateDatabaseFixture : IAsyncLifetime
{
    private const string CorpusFileDateFormat = "yyMMdd";

    private static readonly Regex _corpusFileNamePattern = new(@"^u_ex(\d{6})_x_", RegexOptions.Compiled);

    /// <summary>
    /// Gets the full path of the shared, harvested aggregate database file.
    /// </summary>
    public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        foreach (var corpusDate in DiscoverCorpusDates())
        {
            var result = await CompiledProgram.RunAsync(corpusDate, DatabasePath).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Building the harvested aggregate database fixture failed for {corpusDate.ToString(CorpusFileDateFormat, CultureInfo.InvariantCulture)} " +
                    $"(exit code {result.ExitCode}):{Environment.NewLine}{result.StandardError}");
            }
        }
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Discovers every calendar date to build the fixture for: the full inclusive range between the earliest and
    /// latest date embedded in a corpus file name, not merely the dates that have a file of their own. A
    /// u_ex&lt;date&gt; file can contain lines timestamped for the adjacent day (see the corpus's own
    /// cross-date-line behavior), so a date could have real rows without ever being a file name's own date -
    /// building the full range, gaps included, ensures the shipped executable still gets a chance to pick up
    /// such a date's rows from whichever file actually holds them.
    /// </summary>
    private static DateOnly[] DiscoverCorpusDates()
    {
        var fileNameDates = Directory.GetFiles(CompiledProgram.CorpusDirectory, "u_ex*.log", SearchOption.TopDirectoryOnly)
            .Select(path => _corpusFileNamePattern.Match(Path.GetFileName(path)))
            .Where(match => match.Success)
            .Select(match => DateOnly.ParseExact(match.Groups[1].Value, CorpusFileDateFormat, CultureInfo.InvariantCulture))
            .ToArray();

        var earliestDate = fileNameDates.Min();
        var latestDate = fileNameDates.Max();
        var dayCount = latestDate.DayNumber - earliestDate.DayNumber + 1;

        return Enumerable.Range(0, dayCount).Select(earliestDate.AddDays).ToArray();
    }
}
