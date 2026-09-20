namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One device's summed hit count within an arbitrary date range - the shape a device section's
/// Top-50 Leaderboard needs.
/// </summary>
public sealed record DeviceHitsLeaderboardRow
{
    /// <summary>
    /// Gets the grouping key: the casefolded device id.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets the device's username (earliest date, then alphabetical, among its attributed rows), or
    /// <see langword="null"/> when the device has never been attributed.
    /// </summary>
    public required string? Username { get; init; }

    /// <summary>
    /// Gets the summed hit count this row is ranked by.
    /// </summary>
    public required int Hits { get; init; }
}
