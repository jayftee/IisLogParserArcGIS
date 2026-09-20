using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Computes the by-user-agent aggregate rows for one local date: one row per normalized <c>cs(User-Agent)</c>,
/// holding the accumulated hits and time-taken for that day, per the rules in <c>requirements.md</c>.
/// </summary>
public static class ByUserAgentAggregator
{
    private const int UserAgentMaxLength = 1024;

    /// <summary>
    /// Aggregates <paramref name="requests"/> into by-user-agent rows for <paramref name="localDate"/>. Requests
    /// for any other local date (e.g. lines from a UTC-adjacent file pulled in only to cover a boundary hour) are
    /// ignored.
    /// </summary>
    /// <param name="requests">The normalized requests to aggregate, spanning any number of local dates.</param>
    /// <param name="localDate">The local date to produce rows for.</param>
    /// <returns>One row per distinct normalized <c>cs(User-Agent)</c> seen on <paramref name="localDate"/>.</returns>
    public static IReadOnlyList<ByUserAgentAggregateRow> Aggregate(IEnumerable<NormalizedLogRequest> requests, DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Where(request => request.LocalDate == localDate)
            .GroupBy(request => Normalize(request.UserAgent))
            .Select(group => new ByUserAgentAggregateRow
            {
                LocalDate = localDate,
                UserAgent = group.Key,
                TimeTakenSecond = group.Sum(request => request.TimeTakenMilliseconds / 1000.0),
                Hits = group.Count(),
            })
            .ToArray();
    }

    private static string Normalize(string userAgent)
    {
#pragma warning disable CA1308 // Lowercasing is the normalized storage form required by the spec, not a security comparison.
        var decodedAndLowered = userAgent.Replace('+', ' ').ToLowerInvariant();
#pragma warning restore CA1308
        return decodedAndLowered.Length > UserAgentMaxLength ? decodedAndLowered[..UserAgentMaxLength] : decodedAndLowered;
    }
}
