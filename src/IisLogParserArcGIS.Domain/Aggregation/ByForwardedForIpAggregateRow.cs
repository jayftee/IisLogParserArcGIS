namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-forwarded-for-IP aggregate: the accumulated hits and time-taken for every request
/// sharing a normalized <c>X-Forwarded-For</c> client IP on a given local date.
/// </summary>
public sealed record ByForwardedForIpAggregateRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the grouping key: the first <c>X-Forwarded-For</c> value, validated as an IP address and truncated
    /// to 48 characters.
    /// </summary>
    public required string ForwardedForIp { get; init; }

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values, converted from milliseconds to seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched <see cref="ForwardedForIp"/>.
    /// </summary>
    public required int Hits { get; init; }
}
