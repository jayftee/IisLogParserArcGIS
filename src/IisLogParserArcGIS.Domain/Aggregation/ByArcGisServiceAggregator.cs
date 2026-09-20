using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Computes the by-ArcGIS-Server-service aggregate rows for one local date: one row per
/// <see cref="ArcGisServiceIdentity"/>, holding the accumulated hits, successful/failed hits, and time-taken for
/// that day, per the rules in <c>requirements.md</c>. Requests that are not a genuine ArcGIS Server REST service
/// call (per <see cref="ArcGisServiceIdentityParser"/>) - including admin and Portal traffic - are excluded.
/// </summary>
public static class ByArcGisServiceAggregator
{
    private const int SuccessfulStatusCeiling = 400;

    /// <summary>
    /// Aggregates <paramref name="requests"/> into by-ArcGIS-Server-service rows for <paramref name="localDate"/>.
    /// Requests for any other local date (e.g. lines from a UTC-adjacent file pulled in only to cover a boundary
    /// hour) are ignored, as are requests that are not a genuine ArcGIS Server REST service call.
    /// </summary>
    /// <param name="requests">The normalized requests to aggregate, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to produce rows for.</param>
    /// <returns>One row per distinct ArcGIS Server service identity seen on <paramref name="localDate"/>.</returns>
    public static IReadOnlyList<ByArcGisServiceAggregateRow> Aggregate(IEnumerable<NormalizedLogRequest> requests, DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Where(request => request.LocalDate == localDate)
            .Select(request => (Request: request, Matched: ArcGisServiceIdentityParser.TryParse(request.UriStem, out var identity), Identity: identity))
            .Where(parsed => parsed.Matched)
            .GroupBy(parsed => parsed.Identity!)
            .Select(group => new ByArcGisServiceAggregateRow
            {
                LocalDate = localDate,
                Site = group.Key.Site,
                Folder = group.Key.Folder,
                ServiceName = group.Key.ServiceName,
                ServiceType = group.Key.ServiceType,
                SuccessfulTimeTakenSecond = group.Where(parsed => parsed.Request.Status < SuccessfulStatusCeiling).Sum(parsed => parsed.Request.TimeTakenMilliseconds / 1000.0),
                FailedTimeTakenSecond = group.Where(parsed => parsed.Request.Status >= SuccessfulStatusCeiling).Sum(parsed => parsed.Request.TimeTakenMilliseconds / 1000.0),
                Hits = group.Count(),
                SuccessfulHits = group.Count(parsed => parsed.Request.Status < SuccessfulStatusCeiling),
                FailedHits = group.Count(parsed => parsed.Request.Status >= SuccessfulStatusCeiling),
            })
            .ToArray();
    }
}
