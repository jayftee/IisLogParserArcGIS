using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Computes the by-Portal-item aggregate rows for one local date: one row per distinct portal item id (per
/// <see cref="PortalItemIdentityParser"/>), holding the accumulated hits, successful/failed hits, and
/// time-taken for that day, per the rules in ticket 18. Requests that are not a genuine Portal item-access
/// request are excluded.
/// </summary>
public static class ByPortalItemAggregator
{
    private const int SuccessfulStatusCeiling = 400;

    /// <summary>
    /// Aggregates <paramref name="requests"/> into by-Portal-item rows for <paramref name="localDate"/>. Requests
    /// for any other local date (e.g. lines from a UTC-adjacent file pulled in only to cover a boundary hour) are
    /// ignored, as are requests that are not a genuine Portal item-access request.
    /// </summary>
    /// <param name="requests">The normalized requests to aggregate, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to produce rows for.</param>
    /// <param name="portalWebAdaptorName">The configured Portal Web Adaptor name (case-insensitive match).</param>
    /// <returns>One row per distinct portal item id seen on <paramref name="localDate"/>.</returns>
    public static IReadOnlyList<ByPortalItemAggregateRow> Aggregate(
        IEnumerable<NormalizedLogRequest> requests,
        DateOnly localDate,
        string portalWebAdaptorName)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(portalWebAdaptorName);

        return requests
            .Where(request => request.LocalDate == localDate)
            .Select(request => (
                Request: request,
                Matched: PortalItemIdentityParser.TryParse(request.UriStem, portalWebAdaptorName, out var portalItemId),
                PortalItemId: portalItemId))
            .Where(parsed => parsed.Matched)
            .GroupBy(parsed => parsed.PortalItemId!)
            .Select(group => new ByPortalItemAggregateRow
            {
                LocalDate = localDate,
                PortalItemId = group.Key,
                TimeTakenSecond = group.Sum(parsed => parsed.Request.TimeTakenMilliseconds / 1000.0),
                Hits = group.Count(),
                SuccessfulHits = group.Count(parsed => parsed.Request.Status < SuccessfulStatusCeiling),
                FailedHits = group.Count(parsed => parsed.Request.Status >= SuccessfulStatusCeiling),
            })
            .ToArray();
    }
}
