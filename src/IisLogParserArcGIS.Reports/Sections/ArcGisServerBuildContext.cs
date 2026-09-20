namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Everything <see cref="ArcGisServerSectionBuilder"/>'s per-site page-building helpers need, bundled once per
/// Regeneration Run so those helpers don't each thread five-plus independent parameters individually.
/// </summary>
/// <param name="Queries">The query classes this run's ArcGIS Server section reads through.</param>
/// <param name="IncludedRoots">The Reports project's configured Included Root allow-list.</param>
/// <param name="YearStart">This run's resolved calendar-year-to-date start (the "All" range's start).</param>
/// <param name="Last7DaysStart">The start of the most recent 7 calendar days (the "Last 7 Days" range's start).</param>
/// <param name="Today">This run's resolved local "today" (both ranges' shared end).</param>
/// <param name="GeneratedAtUtc">The instant this Regeneration Run started, for every page's footer.</param>
#pragma warning disable CC0042 // Six independent per-run values threaded through every per-site helper; a smaller grouping would just repackage them without benefit.
internal sealed record ArcGisServerBuildContext(
    ArcGisServerQueries Queries,
    IReadOnlyList<string> IncludedRoots,
    DateOnly YearStart,
    DateOnly Last7DaysStart,
    DateOnly Today,
    DateTimeOffset GeneratedAtUtc);
#pragma warning restore CC0042
