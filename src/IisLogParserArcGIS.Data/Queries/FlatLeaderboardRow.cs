namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// One dimension value's summed hits and total time taken across an arbitrary date range - the shape shared by
/// all four flat Leaderboard queries (ticket 17: forwarded-for IP, referer, URI, user agent). <see cref="Value"/>
/// holds whichever dimension the producing query ranked (an IP, a referer, a URI stem, or a user agent) -
/// identically shaped across all four, the same same-shape-different-source-column precedent already used by
/// <see cref="ArcGisServiceHitsLeaderboardRow"/> for the successful/failed-hits Leaderboard pair. Average time
/// taken is a rendering-time computation (<c>TotalTimeTakenSecond / Hits</c>), not carried on this row, since it
/// is undefined when <see cref="Hits"/> is zero - never the case here, since a zero-hit row carries no ranking
/// value and is excluded from every producing query's results.
/// </summary>
public sealed record FlatLeaderboardRow
{
    /// <summary>
    /// Gets the ranked dimension value (an IP, a referer, a URI stem, or a user agent, depending on which query
    /// produced this row).
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the summed hit count across the queried date range.
    /// </summary>
    public required int Hits { get; init; }

    /// <summary>
    /// Gets the summed time taken, in seconds, across the queried date range.
    /// </summary>
    public required double TotalTimeTakenSecond { get; init; }
}
