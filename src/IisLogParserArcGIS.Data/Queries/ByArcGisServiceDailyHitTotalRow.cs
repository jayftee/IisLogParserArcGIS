namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One local date's summed hits, successful hits, and failed hits for a single site - the shape the ArcGIS
/// Server section's per-site successful/failed request evolution charts need: one point per real date the site
/// has data for.
/// </summary>
public sealed record ByArcGisServiceDailyHitTotalRow
{
    /// <summary>
    /// Gets the local date this row's hits were summed for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the summed hit count for this local date, across every service on the site.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the summed successful-hit count for this local date, across every service on the site.
    /// </summary>
    public required int SuccessfulHits { get; init; }

    /// <summary>
    /// Gets the summed failed-hit count for this local date, across every service on the site.
    /// </summary>
    public required int FailedHits { get; init; }
}
