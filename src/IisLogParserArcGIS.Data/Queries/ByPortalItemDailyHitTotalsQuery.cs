using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads day-by-day hit totals across every Portal item directly from
/// <see cref="AggregateTableNames.ByPortalItem"/>, for the Portal section's own successful/failed request-count
/// evolution charts. Unlike <see cref="ByArcGisServiceDailyHitTotalsQuery"/>, no site/Root filter is applied -
/// the table's rows are already scoped to the configured Portal Web Adaptor at ingest time (per ticket 04's
/// resolution), so Portal's figures are computed unconditionally, regardless of the Included Root allow-list.
/// </summary>
public sealed class ByPortalItemDailyHitTotalsQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT local_date AS LocalDate, SUM(hits) AS Hits, SUM(successful_hits) AS SuccessfulHits, SUM(failed_hits) AS FailedHits
        FROM {AggregateTableNames.ByPortalItem}
        WHERE local_date BETWEEN @StartDate AND @EndDate
        GROUP BY local_date
        ORDER BY local_date
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByPortalItemDailyHitTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByPortalItemDailyHitTotalsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets one row per local date within [<paramref name="startDate"/>, <paramref name="endDate"/>] on which
    /// Portal had at least one hit. A date with no hits is simply absent, not a zero-valued row.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <returns>The matching daily totals, ordered by date.</returns>
    public IEnumerable<ByPortalItemDailyHitTotalRow> GetDailyTotals(DateOnly startDate, DateOnly endDate)
    {
        return Connection.Query<ByPortalItemDailyHitTotalRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate });
    }
}
