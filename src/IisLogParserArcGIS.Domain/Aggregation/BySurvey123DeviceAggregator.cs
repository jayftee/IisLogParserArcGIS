using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Computes the by-Survey123-device aggregate rows for one local date, per ticket 21: ticket 20's two-stage
/// attribution scheme (see <see cref="ByFieldMapsDeviceAggregator"/>) for a second app. Every Survey123 request
/// (a user-agent carrying a device id, per <see cref="Survey123DeviceIdParser"/>) is a hit for its device, and
/// each device is attributed to the username of its earliest successful login line (per
/// <see cref="LoginLineParser"/>) that day, or kept with a <see langword="null"/> username when it has none.
/// </summary>
public static class BySurvey123DeviceAggregator
{
    /// <summary>
    /// Aggregates <paramref name="requests"/> into by-Survey123-device rows for <paramref name="localDate"/>.
    /// Requests for any other local date, and requests that are not Survey123 requests, are ignored.
    /// </summary>
    /// <param name="requests">The normalized requests to aggregate, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to produce rows for.</param>
    /// <param name="portalWebAdaptorName">The configured Portal Web Adaptor name (case-insensitive match).</param>
    /// <returns>One row per distinct device id seen on <paramref name="localDate"/>.</returns>
    public static IReadOnlyList<BySurvey123DeviceAggregateRow> Aggregate(
        IEnumerable<NormalizedLogRequest> requests,
        DateOnly localDate,
        string portalWebAdaptorName)
    {
        var deviceRequests = DeviceUsageTally.ExtractDeviceRequests(requests, localDate, Survey123DeviceIdParser.TryParse);

        return DeviceUsageTally.Tally(deviceRequests, portalWebAdaptorName)
            .Select(usage => new BySurvey123DeviceAggregateRow
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
