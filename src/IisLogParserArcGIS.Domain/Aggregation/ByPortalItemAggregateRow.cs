namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-Portal-item aggregate: the accumulated hits, successful/failed hits, and
/// time-taken for every request sharing a <c>portal_item_id</c> (per <see cref="PortalItemIdentityParser"/>) on
/// a given local date.
/// </summary>
public sealed record ByPortalItemAggregateRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the grouping key: the opaque Portal item id, per <see cref="PortalItemIdentityParser"/>.
    /// </summary>
    public required string PortalItemId { get; init; }

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values, converted from milliseconds to seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched this group. Always equals
    /// <see cref="SuccessfulHits"/> + <see cref="FailedHits"/>.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the count of the group's hits whose <c>sc-status</c> is less than 400.
    /// </summary>
    public required int SuccessfulHits { get; init; }

    /// <summary>
    /// Gets the count of the group's hits whose <c>sc-status</c> is 400 or greater.
    /// </summary>
    public required int FailedHits { get; init; }
}
