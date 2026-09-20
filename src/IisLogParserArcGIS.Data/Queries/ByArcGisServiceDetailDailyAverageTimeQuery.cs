using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads day-by-day successful/failed time-taken totals for a single ArcGIS Server service directly from
/// <see cref="AggregateTableNames.ByArcGisService"/>, for that service's own Detail Page (ticket 13). Deliberately
/// a separate query from <see cref="ByArcGisServiceDetailDailyHitTotalsQuery"/> - per ticket 11's acceptance
/// criteria (carried forward to every Detail Page), an average-time chart recomputes its own per-day sums rather
/// than reusing the hit-count query's rows.
/// </summary>
public sealed class ByArcGisServiceDetailDailyAverageTimeQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT local_date AS LocalDate,
            SUM(successful_time_taken_second) AS SuccessfulTimeTakenSecond, SUM(successful_hits) AS SuccessfulHits,
            SUM(failed_time_taken_second) AS FailedTimeTakenSecond, SUM(failed_hits) AS FailedHits
        FROM {AggregateTableNames.ByArcGisService}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND site = @Site AND folder IS @Folder
            AND service_name = @ServiceName AND service_type = @ServiceType
        GROUP BY local_date
        ORDER BY local_date
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByArcGisServiceDetailDailyAverageTimeQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByArcGisServiceDetailDailyAverageTimeQuery(IDbConnection connection)
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
    public IEnumerable<ByArcGisServiceDailyAverageTimeRow> GetDailyAverageTimeTotals(DateOnly startDate, DateOnly endDate, ArcGisServiceIdentity service)
    {
        ArgumentNullException.ThrowIfNull(service);

        return Connection.Query<ByArcGisServiceDailyAverageTimeRow>(
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
