namespace IisLogParserArcGIS.Reports.Tests.TestSupport;

/// <summary>
/// One by-device aggregate row for a test to insert into whichever per-device table a section reports on (see
/// <see cref="WorkingDatabaseCopy.InsertDeviceRows"/>). The Field Maps and Survey123 tables have identical
/// columns, so one row type serves both; time taken equals hits, which is all the tests need.
/// </summary>
public sealed record DeviceTestRow
{
    /// <summary>
    /// Gets the local date the row belongs to.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the device id.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets the device's username, or <see langword="null"/> when unattributed.
    /// </summary>
    public required string? Username { get; init; }

    /// <summary>
    /// Gets the hit count, also used as the time taken in seconds.
    /// </summary>
    public required int Hits { get; init; }
}
