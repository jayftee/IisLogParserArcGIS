namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-user-agent aggregate: the accumulated hits and time-taken for every request sharing a
/// normalized <c>cs(User-Agent)</c> on a given local date.
/// </summary>
public sealed record ByUserAgentAggregateRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the grouping key: the raw <c>cs(User-Agent)</c> value with <c>+</c> decoded to space, lowercased, and
    /// truncated to 1024 characters.
    /// </summary>
    public required string UserAgent { get; init; }

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values, converted from milliseconds to seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched <see cref="UserAgent"/>.
    /// </summary>
    public required int Hits { get; init; }
}
