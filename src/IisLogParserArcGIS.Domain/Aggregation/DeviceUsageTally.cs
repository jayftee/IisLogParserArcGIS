using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// The two-stage-attribution logic shared by every by-device aggregator (tickets 20 and 21): stage 1 tallies each
/// device's requests for one local date while collecting a RAM-only device-to-username login list from that
/// day's login lines, then stamps each tally with its device's username. Only how a device id is read from a
/// user-agent differs between apps, so that is passed in.
/// </summary>
internal static class DeviceUsageTally
{
    private const int SuccessfulStatusCeiling = 400;

    /// <summary>
    /// Keeps the requests of <paramref name="localDate"/> whose user-agent carries a device id per
    /// <paramref name="deviceIdParser"/>, paired with that id.
    /// </summary>
    /// <param name="requests">The normalized requests, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to keep.</param>
    /// <param name="deviceIdParser">The app's device-id rule.</param>
    /// <returns>The device requests, in input order.</returns>
    internal static IReadOnlyList<DeviceRequest> ExtractDeviceRequests(
        IEnumerable<NormalizedLogRequest> requests,
        DateOnly localDate,
        DeviceIdParser deviceIdParser)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Where(request => request.LocalDate == localDate)
            .Select(request => (
                Request: request,
                Matched: deviceIdParser(request.UserAgent, out var deviceId),
                DeviceId: deviceId))
            .Where(parsed => parsed.Matched)
            .Select(parsed => new DeviceRequest(parsed.Request, parsed.DeviceId!))
            .ToArray();
    }

    /// <summary>
    /// Tallies <paramref name="deviceRequests"/> per device and attributes each device to the username of its
    /// earliest successful login line (by timestamp) that day. A device with no login keeps a
    /// <see langword="null"/> username - it is never dropped.
    /// </summary>
    /// <param name="deviceRequests">The device requests of a single local date.</param>
    /// <param name="portalWebAdaptorName">The configured Portal Web Adaptor name (case-insensitive match).</param>
    /// <returns>One tally per distinct device id.</returns>
    internal static IReadOnlyList<DeviceUsage> Tally(IReadOnlyList<DeviceRequest> deviceRequests, string portalWebAdaptorName)
    {
        ArgumentNullException.ThrowIfNull(portalWebAdaptorName);

        var loginList = BuildLoginList(deviceRequests, portalWebAdaptorName);

        return deviceRequests
            .GroupBy(deviceRequest => deviceRequest.DeviceId)
            .Select(group => new DeviceUsage
            {
                DeviceId = group.Key,
                Username = loginList.GetValueOrDefault(group.Key),
                TimeTakenSecond = group.Sum(deviceRequest => deviceRequest.Request.TimeTakenMilliseconds / 1000.0),
                Hits = group.Count(),
            })
            .ToArray();
    }

    private static Dictionary<string, string> BuildLoginList(IReadOnlyList<DeviceRequest> deviceRequests, string portalWebAdaptorName)
    {
        var loginList = new Dictionary<string, string>();
        foreach (var (request, deviceId) in deviceRequests.OrderBy(deviceRequest => deviceRequest.Request.UtcDateTime))
        {
            if (request.Status < SuccessfulStatusCeiling && LoginLineParser.TryParse(request.UriStem, portalWebAdaptorName, out var username))
            {
                loginList.TryAdd(deviceId, username);
            }
        }

        return loginList;
    }
}
