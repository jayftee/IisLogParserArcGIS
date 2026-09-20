using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Computes the by-forwarded-for-IP aggregate rows for one local date: one row per normalized
/// <c>X-Forwarded-For</c> client IP, holding the accumulated hits and time-taken for that day, per the rules in
/// <c>requirements.md</c>.
/// </summary>
public static class ByForwardedForIpAggregator
{
    /// <summary>
    /// Aggregates <paramref name="requests"/> into by-forwarded-for-IP rows for <paramref name="localDate"/>.
    /// Requests for any other local date (e.g. lines from a UTC-adjacent file pulled in only to cover a boundary
    /// hour) are ignored. A request whose governing header never declared <c>X-Forwarded-For</c> is silently
    /// excluded - no row, no error - since there is no column to group it under.
    /// </summary>
    /// <param name="requests">The normalized requests to aggregate, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to produce rows for.</param>
    /// <returns>One row per distinct normalized forwarded-for IP seen on <paramref name="localDate"/>.</returns>
    public static IReadOnlyList<ByForwardedForIpAggregateRow> Aggregate(IEnumerable<NormalizedLogRequest> requests, DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Where(request => request.LocalDate == localDate && request.ForwardedFor is not null)
            .GroupBy(request => ForwardedForIpNormalizer.Normalize(request.ForwardedFor!))
            .Select(group => new ByForwardedForIpAggregateRow
            {
                LocalDate = localDate,
                ForwardedForIp = group.Key,
                TimeTakenSecond = group.Sum(request => request.TimeTakenMilliseconds / 1000.0),
                Hits = group.Count(),
            })
            .ToArray();
    }
}
