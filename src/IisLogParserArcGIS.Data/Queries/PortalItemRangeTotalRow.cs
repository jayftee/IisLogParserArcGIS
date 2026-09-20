namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One Portal item's summed totals across an arbitrary date range - the shape the Portal section's section-wide
/// Complete View needs: one row per real Portal item. Average time taken is a rendering-time computation
/// (<c>TotalTimeTakenSecond / Hits</c>), not carried on this row, since it is undefined when <see cref="Hits"/>
/// is zero.
/// </summary>
public sealed record PortalItemRangeTotalRow
{
    /// <summary>
    /// Gets the grouping key: the opaque Portal item id.
    /// </summary>
    public required string PortalItemId { get; init; }

    /// <summary>
    /// Gets the summed hit count across the queried date range.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the summed successful-hit count across the queried date range.
    /// </summary>
    public required int SuccessfulHits { get; init; }

    /// <summary>
    /// Gets the summed failed-hit count across the queried date range.
    /// </summary>
    public required int FailedHits { get; init; }

    /// <summary>
    /// Gets the summed time taken, in seconds, across the queried date range.
    /// </summary>
    public required double TotalTimeTakenSecond { get; init; }
}
