using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Computes the by-referer-and-URI aggregate rows for one local date: one row per normalized
/// (<c>cs(Referer)</c>, <c>cs-uri-stem</c>) pair, holding the accumulated hits and time-taken for that day, per
/// the rules in <c>requirements.md</c>. The grouping key composes <see cref="RefererNormalizer"/> and
/// <see cref="UriStemNormalizer"/> rather than re-deriving either normalization.
/// </summary>
public static class ByRefererAndUriAggregator
{
    /// <summary>
    /// Aggregates <paramref name="requests"/> into by-referer-and-URI rows for <paramref name="localDate"/>.
    /// Requests for any other local date (e.g. lines from a UTC-adjacent file pulled in only to cover a boundary
    /// hour) are ignored.
    /// </summary>
    /// <param name="requests">The normalized requests to aggregate, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to produce rows for.</param>
    /// <returns>
    /// One row per distinct normalized (referer, URI) pair seen on <paramref name="localDate"/>.
    /// </returns>
    public static IReadOnlyList<ByRefererAndUriAggregateRow> Aggregate(IEnumerable<NormalizedLogRequest> requests, DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Where(request => request.LocalDate == localDate)
            .GroupBy(request => (
                Referer: RefererNormalizer.Normalize(request.Referer),
                UriStem: UriStemNormalizer.Normalize(request.UriStem)))
            .Select(group => new ByRefererAndUriAggregateRow
            {
                LocalDate = localDate,
                Referer = group.Key.Referer,
                UriStem = group.Key.UriStem,
                TimeTakenSecond = group.Sum(request => request.TimeTakenMilliseconds / 1000.0),
                Hits = group.Count(),
            })
            .ToArray();
    }
}
