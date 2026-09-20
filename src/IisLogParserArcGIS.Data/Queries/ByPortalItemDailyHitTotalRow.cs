namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One local date's summed hits, successful hits, and failed hits across every Portal item - the shape the
/// Portal section's own successful/failed request-count evolution charts need: one point per real date Portal
/// has data for.
/// </summary>
public sealed record ByPortalItemDailyHitTotalRow
{
    /// <summary>
    /// Gets the local date this row's hits were summed for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the summed hit count for this local date, across every Portal item.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the summed successful-hit count for this local date, across every Portal item.
    /// </summary>
    public required int SuccessfulHits { get; init; }

    /// <summary>
    /// Gets the summed failed-hit count for this local date, across every Portal item.
    /// </summary>
    public required int FailedHits { get; init; }
}
