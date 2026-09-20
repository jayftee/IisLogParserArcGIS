namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-Survey123-device aggregate: the accumulated hits and time-taken for every Survey123
/// request sharing a device id on a given local date, plus the username the device is known to belong to.
/// </summary>
public sealed record BySurvey123DeviceAggregateRow : IUsernameAttributedRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the grouping key: the casefolded 32-hex device id, per <see cref="Survey123DeviceIdParser"/>.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets the casefolded username of the user who signed in on this device, or <see langword="null"/> when no
    /// login has been matched to the device yet.
    /// </summary>
    public required string? Username { get; init; }

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values, converted from milliseconds to seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched this group, including the login lines themselves.
    /// </summary>
    public required int Hits { get; init; }
}
