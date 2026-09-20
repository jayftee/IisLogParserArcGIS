namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One folder/service/type's summed hit count within an arbitrary date range, for a single site - the shape the
/// ArcGIS Server section's by-successful-hits and by-failed-hits Top-50 Leaderboards both need. Which hit count
/// <see cref="Hits"/> holds (successful or failed) depends on which query produced the row.
/// </summary>
public sealed record ArcGisServiceHitsLeaderboardRow
{
    /// <summary>
    /// Gets the grouping key's folder, or <see langword="null"/> for a folderless service.
    /// </summary>
    public string? Folder { get; init; }

    /// <summary>
    /// Gets the grouping key's service name.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the grouping key's service type.
    /// </summary>
    public required string ServiceType { get; init; }

    /// <summary>
    /// Gets the summed hit count this row is ranked by. Always greater than zero, since a row with no matching
    /// hits carries no ranking value and is excluded from the query's results.
    /// </summary>
    public required int Hits { get; init; }
}
