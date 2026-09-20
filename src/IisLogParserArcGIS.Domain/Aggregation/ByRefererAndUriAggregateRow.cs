namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-referer-and-URI aggregate: the accumulated hits and time-taken for every request
/// sharing a normalized (<c>cs(Referer)</c>, <c>cs-uri-stem</c>) pair on a given local date.
/// </summary>
public sealed record ByRefererAndUriAggregateRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the referer half of the grouping key, normalized per <see cref="RefererNormalizer"/>.
    /// </summary>
    public required string Referer { get; init; }

#pragma warning disable CA1054, CA1056 // A raw cs-uri-stem log field value, not a well-formed System.Uri.
    /// <summary>
    /// Gets the URI half of the grouping key, normalized per <see cref="UriStemNormalizer"/>.
    /// </summary>
    public required string UriStem { get; init; }
#pragma warning restore CA1054, CA1056

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values, converted from milliseconds to seconds.
    /// </summary>
    public required double TimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched the (<see cref="Referer"/>, <see cref="UriStem"/>) pair.
    /// </summary>
    public required int Hits { get; init; }
}
