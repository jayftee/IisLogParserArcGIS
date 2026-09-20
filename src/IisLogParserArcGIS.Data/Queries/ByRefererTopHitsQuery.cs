using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads the top 500 referers, ranked by summed hits over an arbitrary date range - one of the four flat,
/// standalone Leaderboards (ticket 17). The by-referer aggregate's row grain already matches what this needs, so
/// no SQL view is required (per ticket 03's resolution). Excludes the <c>"-"</c> empty-referer placeholder that
/// <see cref="IisLogParserArcGIS.Domain.Aggregation.RefererNormalizer"/> already normalizes every empty or
/// literal <c>"-"</c> <c>cs(Referer)</c> value to upstream, at aggregation time - this query filters that
/// existing placeholder out, it does not re-derive or re-normalize it.
/// </summary>
public sealed class ByRefererTopHitsQuery : AggregateQueryBase
{
    private const string LeaderboardSize = "500";
    private const string EmptyRefererPlaceholder = "-";

    private const string SelectSql =
        $"""
        SELECT referer AS Value, SUM(hits) AS Hits, SUM(time_taken_second) AS TotalTimeTakenSecond
        FROM {AggregateTableNames.ByReferer}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND referer <> @EmptyRefererPlaceholder
        GROUP BY referer
        ORDER BY Hits DESC
        LIMIT {LeaderboardSize}
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByRefererTopHitsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByRefererTopHitsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets up to the top 500 referers, ranked descending by summed hits within [<paramref name="startDate"/>,
    /// <paramref name="endDate"/>], excluding the <c>"-"</c> empty-referer placeholder. Fewer than 500 rows come
    /// back, with no placeholder, when fewer than 500 distinct referers appear in range - no secondary tie-break
    /// is applied among rows sharing the same hit count.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <returns>Up to 500 rows, ranked descending by hits.</returns>
    public IEnumerable<FlatLeaderboardRow> GetTop500(DateOnly startDate, DateOnly endDate)
    {
        return Connection.Query<FlatLeaderboardRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, EmptyRefererPlaceholder });
    }
}
