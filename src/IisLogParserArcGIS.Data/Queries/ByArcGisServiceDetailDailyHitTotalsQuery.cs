using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads day-by-day hit totals for a single ArcGIS Server service directly from
/// <see cref="AggregateTableNames.ByArcGisService"/>, for that service's own Detail Page (ticket 13). Reuses
/// <see cref="ByArcGisServiceDailyHitTotalRow"/>'s shape - identical to <see cref="ByArcGisServiceDailyHitTotalsQuery"/>'s
/// own row, just scoped down to one (site, folder, service_name, service_type) tuple instead of a whole site. No
/// view needed: this tuple is already the base table's own grain, so there is no dimension left to collapse
/// across (per ticket 03's resolution, a view is reserved for genuine cross-entity collapses).
/// </summary>
public sealed class ByArcGisServiceDetailDailyHitTotalsQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT local_date AS LocalDate, SUM(hits) AS Hits, SUM(successful_hits) AS SuccessfulHits, SUM(failed_hits) AS FailedHits
        FROM {AggregateTableNames.ByArcGisService}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND site = @Site AND folder IS @Folder
            AND service_name = @ServiceName AND service_type = @ServiceType
        GROUP BY local_date
        ORDER BY local_date
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByArcGisServiceDetailDailyHitTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByArcGisServiceDetailDailyHitTotalsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets one row per local date within [<paramref name="startDate"/>, <paramref name="endDate"/>] on which
    /// <paramref name="service"/> had at least one hit. A date with no hits is simply absent, not a zero-valued
    /// row - the caller must not zero-pad missing dates back in (per ticket 13's acceptance criteria).
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="service">The service to restrict results to.</param>
    /// <returns>The matching daily totals, ordered by date.</returns>
    public IEnumerable<ByArcGisServiceDailyHitTotalRow> GetDailyTotals(DateOnly startDate, DateOnly endDate, ArcGisServiceIdentity service)
    {
        ArgumentNullException.ThrowIfNull(service);

        return Connection.Query<ByArcGisServiceDailyHitTotalRow>(
            SelectSql,
            new
            {
                StartDate = startDate,
                EndDate = endDate,
                service.Site,
                service.Folder,
                service.ServiceName,
                service.ServiceType,
            });
    }
}
