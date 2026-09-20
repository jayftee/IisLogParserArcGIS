using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads per-(site, folder, service, type) totals directly from <see cref="AggregateTableNames.ByArcGisService"/>
/// across every Included Root, for the ArcGIS Server section's section-wide Complete View. The by-ArcGIS-
/// Server-service aggregate's row grain already matches what this needs (summing only across the date range, not
/// across any finer dimension), so no SQL view is required - the same reasoning ticket 03 already established
/// for <see cref="ByRootRangeTotalsQuery"/>.
/// </summary>
public sealed class ByArcGisServiceRangeTotalsQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT site AS Site, folder AS Folder, service_name AS ServiceName, service_type AS ServiceType,
            SUM(hits) AS Hits, SUM(successful_hits) AS SuccessfulHits, SUM(failed_hits) AS FailedHits,
            SUM(successful_time_taken_second) + SUM(failed_time_taken_second) AS TotalTimeTakenSecond
        FROM {AggregateTableNames.ByArcGisService}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND site IN @IncludedRoots
        GROUP BY site, folder, service_name, service_type
        ORDER BY site, folder, service_name, service_type
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByArcGisServiceRangeTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByArcGisServiceRangeTotalsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets one row per (site, folder, service, type) combination with at least one hit within
    /// [<paramref name="startDate"/>, <paramref name="endDate"/>], restricted to <paramref name="includedRoots"/>.
    /// A combination with no hits in range is simply absent, not a zero-valued row.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="includedRoots">The sites (Roots) to restrict results to.</param>
    /// <returns>The matching range totals, ordered by site, then folder, service, and type.</returns>
    public IEnumerable<ArcGisServiceRangeTotalRow> GetRangeTotals(DateOnly startDate, DateOnly endDate, IReadOnlyCollection<string> includedRoots)
    {
        ArgumentNullException.ThrowIfNull(includedRoots);

        return Connection.Query<ArcGisServiceRangeTotalRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, IncludedRoots = includedRoots });
    }
}
