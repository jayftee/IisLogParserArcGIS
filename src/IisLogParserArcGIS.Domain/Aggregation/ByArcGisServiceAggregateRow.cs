namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// One grouped row of the by-ArcGIS-Server-service aggregate: the accumulated hits, successful/failed hits, and
/// time-taken for every request sharing an <see cref="ArcGisServiceIdentity"/> on a given local date.
/// </summary>
public sealed record ByArcGisServiceAggregateRow
{
    /// <summary>
    /// Gets the local date this row was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the grouping key's site, per <see cref="ArcGisServiceIdentity.Site"/>.
    /// </summary>
    public required string Site { get; init; }

    /// <summary>
    /// Gets the grouping key's folder, or <see langword="null"/> for a folderless service.
    /// </summary>
    public string? Folder { get; init; }

    /// <summary>
    /// Gets the grouping key's service name.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the grouping key's service type.
    /// </summary>
    public required string ServiceType { get; init; }

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values for hits whose <c>sc-status</c> is less than 400,
    /// converted from milliseconds to seconds.
    /// </summary>
    public required double SuccessfulTimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the sum of the group's <c>time-taken</c> values for hits whose <c>sc-status</c> is 400 or greater,
    /// converted from milliseconds to seconds.
    /// </summary>
    public required double FailedTimeTakenSecond { get; init; }

    /// <summary>
    /// Gets the count of log lines that matched this group. Always equals
    /// <see cref="SuccessfulHits"/> + <see cref="FailedHits"/>.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the count of the group's hits whose <c>sc-status</c> is less than 400.
    /// </summary>
    public required int SuccessfulHits { get; init; }

    /// <summary>
    /// Gets the count of the group's hits whose <c>sc-status</c> is 400 or greater.
    /// </summary>
    public required int FailedHits { get; init; }
}
