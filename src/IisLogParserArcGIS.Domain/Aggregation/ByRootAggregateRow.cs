namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-root aggregate: the accumulated hits and time-taken for every request sharing a
/// normalized root on a given local date.
/// </summary>
public sealed record ByRootAggregateRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the grouping key: the <see cref="RootNormalizer"/>-normalized root.
    /// </summary>
    public required string Root { get; init; }

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values, converted from milliseconds to seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched <see cref="Root"/>.
    /// </summary>
    public required int Hits { get; init; }
}
