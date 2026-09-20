namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One Root's summed hits and total time taken across an arbitrary date range - the shape the Summary section's
/// Complete View table needs. Average time taken is a rendering-time computation
/// (<c>TotalTimeTakenSecond / Hits</c>), not carried on this row, since it is undefined when <see cref="Hits"/>
/// is zero.
/// </summary>
public sealed record ByRootRangeTotalRow
{
    /// <summary>
    /// Gets the Root this row's totals were summed for.
    /// </summary>
    public required string Root { get; init; }

    /// <summary>
    /// Gets the summed hit count across the queried date range.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the summed time taken, in seconds, across the queried date range.
    /// </summary>
    public required double TotalTimeTakenSecond { get; init; }
}
