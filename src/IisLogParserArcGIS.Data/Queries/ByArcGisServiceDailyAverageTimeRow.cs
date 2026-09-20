namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One local date's summed successful/failed time-taken and hit counts for a single site - the shape the ArcGIS
/// Server section's per-site average-processing-time evolution charts need. Average time taken is a
/// rendering-time computation (total time taken divided by the matching hit count), not carried on this row,
/// since it is undefined when the matching hit count is zero.
/// </summary>
public sealed record ByArcGisServiceDailyAverageTimeRow
{
    /// <summary>
    /// Gets the local date this row's totals were summed for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the summed time taken, in seconds, for successful hits on this local date.
    /// </summary>
    public required double SuccessfulTimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the summed successful-hit count for this local date.
    /// </summary>
    public required int SuccessfulHits { get; init; }

    /// <summary>
    /// Gets the summed time taken, in seconds, for failed hits on this local date.
    /// </summary>
    public required double FailedTimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the summed failed-hit count for this local date.
    /// </summary>
    public required int FailedHits { get; init; }
}
