namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One device's summed totals across an arbitrary date range - the shape a device section's
/// Complete View needs: one row per device. Average time taken is a rendering-time computation
/// (<c>TotalTimeTakenSecond / Hits</c>), not carried on this row.
/// </summary>
public sealed record DeviceRangeTotalRow
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
    /// Gets the summed hit count across the queried date range.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the summed time taken, in seconds, across the queried date range.
    /// </summary>
    public required double TotalTimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the most recent local date within the queried range on which the device had any hit.
    /// </summary>
    public required DateOnly LastSeen { get; init; }
}
