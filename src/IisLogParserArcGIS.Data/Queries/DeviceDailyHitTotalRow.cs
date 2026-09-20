namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One local date's summed hits for a single device (Field Maps or Survey123) - the shape the device's own Detail Page chart
/// needs: one point per real date the device has data for.
/// </summary>
public sealed record DeviceDailyHitTotalRow
{
    /// <summary>
    /// Gets the local date this row's hits were summed for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the summed hit count for this local date.
    /// </summary>
    public required int Hits { get; init; }
}
