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
    /// Runs one Regeneration Run: resolves this run's year-to-date boundaries, (re)creates
    /// <paramref name="outputDirectory"/> empty, and builds every Dashboard section into it (Summary, Portal,
    /// ArcGIS Server, Fieldmaps, Survey123, and the flat Leaderboard section). Always a full rebuild - a prior run's output is replaced
    /// outright, never merged with or appended to.
    /// </summary>
    /// <param name="connection">An open connection to the aggregate database that later stages of this run query.</param>
    /// <param name="settings">The Reports project's bound configuration.</param>
    /// <param name="timeProvider">The clock this run resolves "today" and "this year" from.</param>
    /// <param name="outputDirectory">The directory the Dashboard is (re)generated into.</param>
    /// <returns>This run's resolved year-to-date boundaries, for later stages of the same run to reuse.</returns>
    /// <exception cref="ArgumentException"><paramref name="outputDirectory"/> resolves to a drive root.</exception>
#pragma warning disable CC0042 // Four independent inputs to this project's one entry point (spec.md's "primary seam"); a parameter object would add indirection without benefit.
    public static RegenerationRunBoundaries Run(SqliteConnection connection, ReportsSettings settings, TimeProvider timeProvider, string outputDirectory)
#pragma warning restore CC0042
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, settings.LocalTimeZone);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);

        if (string.Equals(fullOutputDirectory, Path.GetPathRoot(fullOutputDirectory), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Refusing to use '{fullOutputDirectory}' as the Dashboard output directory: it resolves to a drive root, which this run would delete recursively.",
                nameof(outputDirectory));
        }

        if (Directory.Exists(fullOutputDirectory))
        {
            Directory.Delete(fullOutputDirectory, recursive: true);
        }

        Directory.CreateDirectory(fullOutputDirectory);

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
