using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// The one SQL expression every per-device query (Fieldmaps and Survey123) uses to pick a device's username, so
/// the Leaderboard and the Complete View can never disagree. It applies the same rule as the harvest-time
/// back-fill (<see cref="Repositories.DeviceAggregateRepositoryBase{TRow}"/>): among the device's non-<c>NULL</c>
/// rows, the earliest <c>local_date</c>, ties broken alphabetically. It looks across the whole table, not just
/// the queried range, so a device shows the same username on the All and Last 7 Days pages.
/// </summary>
internal static class DeviceUsernameSql
{
    /// <summary>
    /// The alias the enclosing query must give the source that exposes the device's <c>device_id</c>, for
    /// <see cref="Expression"/> to correlate against.
    /// </summary>
    public const string DeviceAlias = "device";

    /// <summary>
    /// Builds a scalar sub-select yielding the username of the device identified by <c>device.device_id</c> in
    /// the enclosing query.
    /// </summary>
    /// <param name="table">The by-device table to look the username up in.</param>
    /// <returns>The SQL expression.</returns>
    public static string Expression(DeviceAggregateTable table)
    {
        return
            $"""
            (SELECT attributed.username
             FROM {table.Name} AS attributed
             WHERE attributed.device_id = {DeviceAlias}.device_id AND attributed.username IS NOT NULL
             ORDER BY attributed.local_date, attributed.username
             LIMIT 1)
            """;
    }
}
