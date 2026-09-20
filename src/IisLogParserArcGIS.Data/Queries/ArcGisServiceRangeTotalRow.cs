namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One folder/service/type's summed totals across an arbitrary date range, for a single site - the shape the
/// ArcGIS Server section's section-wide Complete View needs: one row per real (site, folder, service, type)
/// combination. Average time taken is a rendering-time computation (<c>TotalTimeTakenSecond / Hits</c>), not
/// carried on this row, since it is undefined when <see cref="Hits"/> is zero.
/// </summary>
public sealed record ArcGisServiceRangeTotalRow
{
    /// <summary>
    /// Gets the grouping key's site.
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
    /// Gets the summed hit count across the queried date range.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the summed successful-hit count across the queried date range.
    /// </summary>
    public required int SuccessfulHits { get; init; }

    /// <summary>
    /// Gets the summed failed-hit count across the queried date range.
    /// </summary>
    public required int FailedHits { get; init; }

    /// <summary>
    /// Gets the summed time taken, in seconds (successful and failed combined), across the queried date range.
    /// </summary>
    public required double TotalTimeTakenSecond { get; init; }
}
