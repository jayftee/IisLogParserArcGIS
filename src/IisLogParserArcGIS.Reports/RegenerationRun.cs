using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Rendering;
using IisLogParserArcGIS.Reports.Sections;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Reports;

/// <summary>
/// The Reports project's single entry point: drives one full Regeneration Run against the current contents of
/// the aggregate database, replacing whatever static-HTML Dashboard was previously generated at
/// <c>outputDirectory</c>.
/// </summary>
public static class RegenerationRun
{
    /// <summary>
    /// The name of the marker file every Regeneration Run writes into its output directory. A later run only
    /// deletes and rebuilds a non-empty output directory that carries it, so a mistyped path can never wipe a
    /// directory this tool did not itself generate.
    /// </summary>
    public const string MarkerFileName = ".iis-dashboard";

    /// <summary>
    /// Runs one Regeneration Run: resolves this run's boundaries for <paramref name="year"/>, (re)creates
    /// <paramref name="outputDirectory"/> empty, and builds every Dashboard section into it (Summary, Portal,
    /// ArcGIS Server, Fieldmaps, Survey123, and the flat Leaderboard section). Always a full rebuild - a prior run's output is replaced
    /// outright, never merged with or appended to.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database that later stages of this run query.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="timeProvider">The clock this run resolves "today" from.</param>
    /// <param name="outputDirectory">The directory the Dashboard is (re)generated into.</param>
    /// <param name="year">The calendar year the Dashboard reports on.</param>
    /// <returns>This run's resolved boundaries, for later stages of the same run to reuse.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="outputDirectory"/> resolves to a drive root, or exists, is not empty, and does not contain
    /// <see cref="MarkerFileName"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="year"/> is after the current local year.</exception>
#pragma warning disable CC0042 // Five independent inputs to this project's one entry point (spec.md's "primary seam"); a parameter object would add indirection without benefit.
    public static RegenerationRunBoundaries Run(SqliteConnection connection, ReportsSettings settings, TimeProvider timeProvider, string outputDirectory, int year)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, settings.LocalTimeZone, year);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);

        if (string.Equals(fullOutputDirectory, Path.GetPathRoot(fullOutputDirectory), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Refusing to use '{fullOutputDirectory}' as the Dashboard output directory: it resolves to a drive root, which this run would delete recursively.",
                nameof(outputDirectory));
        }

        if (Directory.Exists(fullOutputDirectory))
        {
            var holdsAnything = Directory.EnumerateFileSystemEntries(fullOutputDirectory).Any();

            if (holdsAnything && File.Exists(Path.Combine(fullOutputDirectory, MarkerFileName)) is false)
            {
                throw new ArgumentException(
                    $"Refusing to use '{fullOutputDirectory}' as the Dashboard output directory: it is not empty and does not contain the '{MarkerFileName}' marker " +
                    "that every Dashboard this tool generates carries, so this tool did not generate it and this run would delete it recursively. " +
                    "Empty the directory, or point the run at a new or empty one.",
                    nameof(outputDirectory));
            }

            Directory.Delete(fullOutputDirectory, recursive: true);
        }

        Directory.CreateDirectory(fullOutputDirectory);
        File.WriteAllText(Path.Combine(fullOutputDirectory, MarkerFileName), string.Empty);

        var generatedAtUtc = timeProvider.GetUtcNow();

        PageShellAssets.WriteTo(fullOutputDirectory);
        SummarySectionBuilder.Build(connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
        PortalSectionBuilder.Build(connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
        PortalCompleteViewBuilder.Build(connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
        PortalItemDetailPageBuilder.Build(connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
        ArcGisServerSectionBuilder.Build(connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
        ArcGisServerCompleteViewBuilder.Build(connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
        ArcGisServiceDetailPageBuilder.Build(connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);

        foreach (var section in DeviceSection.All)
        {
            DeviceLeaderboardSectionBuilder.Build(section, connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
            DeviceCompleteViewBuilder.Build(section, connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
            DeviceDetailPageBuilder.Build(section, connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);
        }

        FlatLeaderboardSectionBuilder.Build(connection, settings, boundaries, generatedAtUtc, fullOutputDirectory);

        return boundaries;
    }
}
