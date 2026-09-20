namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One Portal item's summed hit count within an arbitrary date range - the shape the Portal section's
/// by-successful-hits and by-failed-hits Top-50 Leaderboards both need. Which hit count <see cref="Hits"/> holds
/// (successful or failed) depends on which query produced the row.
/// </summary>
public sealed record PortalItemHitsLeaderboardRow
{
    /// <summary>
    /// Gets the grouping key: the opaque Portal item id.
    /// </summary>
    public required string PortalItemId { get; init; }

    /// <summary>
    /// Gets the summed hit count this row is ranked by. Always greater than zero, since a row with no matching
    /// hits carries no ranking value and is excluded from the query's results.
    /// </summary>
    public required int Hits { get; init; }
}
