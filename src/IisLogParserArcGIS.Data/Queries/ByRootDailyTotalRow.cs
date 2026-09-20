namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One (local date, Root) group's summed hits within an arbitrary date range - the shape the Summary section's
/// per-Root total-requests line chart needs: one point per real (date, Root) combination that has data.
/// </summary>
public sealed record ByRootDailyTotalRow
{
    /// <summary>
    /// Gets the local date this row's hits were summed for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the Root this row's hits were summed for.
    /// </summary>
    public required string Root { get; init; }

    /// <summary>
    /// Gets the summed hit count for this (local date, Root) group.
    /// </summary>
    public required int Hits { get; init; }
}
