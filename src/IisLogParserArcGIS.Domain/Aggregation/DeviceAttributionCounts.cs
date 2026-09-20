namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// How one Harvest Run's devices (Field Maps or Survey123) were attributed to a username, so the run summary can
/// make the (often large) unattributed share visible instead of silent. Counts are per device row for the
/// harvested local date.
/// </summary>
/// <param name="AttributedSameDay">The devices whose own day's login lines gave them a username.</param>
/// <param name="AttributedByBackfill">The devices given a username afterwards, from a login on another date.</param>
/// <param name="Unattributed">The devices that still have no username.</param>
public sealed record DeviceAttributionCounts(int AttributedSameDay, int AttributedByBackfill, int Unattributed)
{
    /// <summary>
    /// Gets the number of distinct devices with any hit of the app on the harvested date.
    /// </summary>
    public int DevicesSeen => AttributedSameDay + AttributedByBackfill + Unattributed;

    /// <summary>
    /// Derives the counts from a date's rows as aggregated (before the cross-date back-fill) and as persisted
    /// (after it).
    /// </summary>
    /// <param name="aggregatedRows">The date's rows as produced by the app's by-device aggregator.</param>
    /// <param name="persistedRows">The same date's rows read back from the database after the back-fill.</param>
    /// <returns>The attribution counts.</returns>
    public static DeviceAttributionCounts Create(
        IReadOnlyCollection<IUsernameAttributedRow> aggregatedRows,
        IReadOnlyCollection<IUsernameAttributedRow> persistedRows)
    {
        ArgumentNullException.ThrowIfNull(aggregatedRows);
        ArgumentNullException.ThrowIfNull(persistedRows);

        var sameDay = aggregatedRows.Count(row => row.Username is not null);
        var unattributed = persistedRows.Count(row => row.Username is null);
        return new DeviceAttributionCounts(sameDay, aggregatedRows.Count - sameDay - unattributed, unattributed);
    }
}
