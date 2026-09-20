namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-URI aggregate: the accumulated hits and time-taken for every request sharing a
/// normalized <c>uri_stem</c> on a given local date.
/// </summary>
public sealed record ByUriAggregateRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

#pragma warning disable CA1054, CA1056 // A raw cs-uri-stem log field value, not a well-formed System.Uri.
    /// <summary>
    /// Gets the grouping key: the raw <c>cs-uri-stem</c> value, truncated to 1024 characters.
    /// </summary>
    public required string UriStem { get; init; }
#pragma warning restore CA1054, CA1056

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values, converted from milliseconds to seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched <see cref="UriStem"/>.
    /// </summary>
    public required int Hits { get; init; }
}
