namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One device's tally for a local date, before it is shaped into an app's aggregate row.
/// </summary>
internal sealed record DeviceUsage
{
    /// <summary>
    /// Gets the casefolded device id.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets the username the device logged in as that day, or <see langword="null"/>.
    /// </summary>
    public required string? Username { get; init; }

    /// <summary>
    /// Gets the sum of the device's <c>time-taken</c> values, in seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of the device's requests, login lines included.
    /// </summary>
    public required int Hits { get; init; }
}
