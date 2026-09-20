namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-referer aggregate: the accumulated hits and time-taken for every request sharing a
/// normalized <c>cs(Referer)</c> on a given local date.
/// </summary>
public sealed record ByRefererAggregateRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the grouping key: the raw <c>cs(Referer)</c> value normalized per <see cref="RefererNormalizer"/>.
    /// </summary>
    public required string Referer { get; init; }

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values, converted from milliseconds to seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched <see cref="Referer"/>.
    /// </summary>
    public required int Hits { get; init; }
}
