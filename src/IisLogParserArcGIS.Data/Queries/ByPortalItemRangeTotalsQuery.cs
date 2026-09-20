using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads per-Portal-item totals directly from <see cref="AggregateTableNames.ByPortalItem"/>, for the Portal
/// section's section-wide Complete View (ticket 15). The by-Portal-item aggregate's row grain already matches
/// what this needs (summing only across the date range, not across any finer dimension), so no SQL view is
/// required - the same reasoning ticket 03 already established for <see cref="ByRootRangeTotalsQuery"/> and
/// <see cref="ByArcGisServiceRangeTotalsQuery"/>. No site/Root filter is applied - the table's rows are already
/// scoped to the configured Portal Web Adaptor at ingest time (per ticket 04's resolution).
/// </summary>
public sealed class ByPortalItemRangeTotalsQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT portal_item_id AS PortalItemId, SUM(hits) AS Hits, SUM(successful_hits) AS SuccessfulHits,
            SUM(failed_hits) AS FailedHits, SUM(time_taken_second) AS TotalTimeTakenSecond
        FROM {AggregateTableNames.ByPortalItem}
        WHERE local_date BETWEEN @StartDate AND @EndDate
        GROUP BY portal_item_id
        ORDER BY portal_item_id
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByPortalItemRangeTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByPortalItemRangeTotalsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets one row per Portal item with at least one hit within [<paramref name="startDate"/>, <paramref name="endDate"/>].
    /// A Portal item with no hits in range is simply absent, not a zero-valued row.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <returns>The matching range totals, ordered by Portal item id.</returns>
    public IEnumerable<PortalItemRangeTotalRow> GetRangeTotals(DateOnly startDate, DateOnly endDate)
    {
        return Connection.Query<PortalItemRangeTotalRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate });
    }
}
