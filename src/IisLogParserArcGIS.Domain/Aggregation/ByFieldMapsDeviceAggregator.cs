using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Computes the by-Field-Maps-device aggregate rows for one local date, per ticket 20. Every Field Maps request
/// (a user-agent carrying a device id, per <see cref="FieldMapsDeviceIdParser"/>) is a hit for its device. While
/// tallying, the day's login lines (per <see cref="LoginLineParser"/>) are collected into an in-memory
/// device-to-username list, and once the day's lines are exhausted each row is attributed to the username its
/// device logged in as that day (the earliest successful login by timestamp, when a device shows several). A
/// device with no login that day keeps a <see langword="null"/> username - it is never dropped; a later back-fill
/// may still attribute it from another date.
/// </summary>
public static class ByFieldMapsDeviceAggregator
{
    /// <summary>
    /// Aggregates <paramref name="requests"/> into by-Field-Maps-device rows for <paramref name="localDate"/>.
    /// Requests for any other local date, and requests that are not Field Maps requests, are ignored.
    /// </summary>
    /// <param name="requests">The normalized requests to aggregate, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to produce rows for.</param>
    /// <param name="portalWebAdaptorName">The configured Portal Web Adaptor name (case-insensitive match).</param>
    /// <returns>One row per distinct device id seen on <paramref name="localDate"/>.</returns>
    public static IReadOnlyList<ByFieldMapsDeviceAggregateRow> Aggregate(
        IEnumerable<NormalizedLogRequest> requests,
        DateOnly localDate,
        string portalWebAdaptorName)
    {
        var deviceRequests = DeviceUsageTally.ExtractDeviceRequests(requests, localDate, FieldMapsDeviceIdParser.TryParse);

        return DeviceUsageTally.Tally(deviceRequests, portalWebAdaptorName)
            .Select(usage => new ByFieldMapsDeviceAggregateRow
            {
                LocalDate = localDate,
                DeviceId = usage.DeviceId,
                Username = usage.Username,
                TimeTakenSecond = usage.TimeTakenSecond,
                Hits = usage.Hits,
            })
            .ToArray();
    }
}
