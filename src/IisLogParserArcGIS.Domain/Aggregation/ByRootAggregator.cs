using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Computes the by-root aggregate rows for one local date: one row per normalized root, holding the accumulated
/// hits and time-taken for that day, per the rules in <c>requirements.md</c>.
/// </summary>
public static class ByRootAggregator
{
    /// <summary>
    /// Aggregates <paramref name="requests"/> into by-root rows for <paramref name="localDate"/>. Requests for
    /// any other local date (e.g. lines from a UTC-adjacent file pulled in only to cover a boundary hour) are
    /// ignored.
    /// </summary>
    /// <param name="requests">The normalized requests to aggregate, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to produce rows for.</param>
    /// <returns>One row per distinct normalized root seen on <paramref name="localDate"/>.</returns>
    public static IReadOnlyList<ByRootAggregateRow> Aggregate(IEnumerable<NormalizedLogRequest> requests, DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Where(request => request.LocalDate == localDate)
            .GroupBy(request => RootNormalizer.Normalize(request.UriStem))
            .Select(group => new ByRootAggregateRow
            {
                LocalDate = localDate,
                Root = group.Key,
                TimeTakenSecond = group.Sum(request => request.TimeTakenMilliseconds / 1000.0),
                Hits = group.Count(),
            })
            .ToArray();
    }
}
