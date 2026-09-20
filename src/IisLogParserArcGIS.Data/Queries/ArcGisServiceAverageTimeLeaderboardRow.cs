namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One folder/service/type's summed time-taken and matching hit count within an arbitrary date range, for a
/// single site - the shape the ArcGIS Server section's by-average-time Top-50 Leaderboards both need. Average
/// time taken is a rendering-time computation (<see cref="TotalTimeTakenSecond"/> divided by <see cref="Hits"/>),
/// not carried on this row. Whether <see cref="TotalTimeTakenSecond"/>/<see cref="Hits"/> reflect successful or
/// failed requests depends on which query produced the row.
/// </summary>
public sealed record ArcGisServiceAverageTimeLeaderboardRow
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
    /// Gets the summed time taken, in seconds, this row is ranked by (as an average against <see cref="Hits"/>).
    /// </summary>
    public required double TotalTimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the summed hit count <see cref="TotalTimeTakenSecond"/> is averaged over. Always greater than zero,
    /// since a row with no matching hits carries no defined average and is excluded from the query's results.
    /// </summary>
    public required int Hits { get; init; }
}
