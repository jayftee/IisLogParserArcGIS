using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads day-by-day hit totals for a single Portal item directly from
/// <see cref="AggregateTableNames.ByPortalItem"/>, for that item's own Detail Page (ticket 16). Reuses
/// <see cref="ByPortalItemDailyHitTotalRow"/>'s shape - identical to <see cref="ByPortalItemDailyHitTotalsQuery"/>'s
/// own row, just scoped down to one <c>portal_item_id</c> instead of every Portal item. No view needed: the
/// (date, item) pair is already the base table's own grain, so there is no dimension left to collapse across (per
/// ticket 03's resolution, a view is reserved for genuine cross-entity collapses).
/// </summary>
public sealed class ByPortalItemDetailDailyHitTotalsQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT local_date AS LocalDate, SUM(hits) AS Hits, SUM(successful_hits) AS SuccessfulHits, SUM(failed_hits) AS FailedHits
        FROM {AggregateTableNames.ByPortalItem}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND portal_item_id = @PortalItemId
        GROUP BY local_date
        ORDER BY local_date
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByPortalItemDetailDailyHitTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByPortalItemDetailDailyHitTotalsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets one row per local date within [<paramref name="startDate"/>, <paramref name="endDate"/>] on which
    /// <paramref name="portalItemId"/> had at least one hit. A date with no hits is simply absent, not a
    /// zero-valued row - the caller must not zero-pad missing dates back in (per ticket 16's acceptance criteria).
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="portalItemId">The Portal item to restrict results to.</param>
    /// <returns>The matching daily totals, ordered by date.</returns>
    public IEnumerable<ByPortalItemDailyHitTotalRow> GetDailyTotals(DateOnly startDate, DateOnly endDate, string portalItemId)
    {
        ArgumentNullException.ThrowIfNull(portalItemId);

        return Connection.Query<ByPortalItemDailyHitTotalRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, PortalItemId = portalItemId });
    }
}
