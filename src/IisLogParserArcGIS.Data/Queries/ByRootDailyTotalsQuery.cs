using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads day-by-day, per-Root hit totals directly from <see cref="AggregateTableNames.ByRoot"/> for the Summary
/// section's total-requests-per-Root line chart. The by-Root aggregate's row grain already matches what this
/// needs, so no view is required (per ADR-0002 and ticket 03's resolution).
/// </summary>
public sealed class ByRootDailyTotalsQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT local_date AS LocalDate, root AS Root, SUM(hits) AS Hits
        FROM {AggregateTableNames.ByRoot}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND root IN @IncludedRoots
        GROUP BY local_date, root
        ORDER BY local_date, root
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByRootDailyTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByRootDailyTotalsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets one row per (local date, Root) with at least one hit within
    /// [<paramref name="startDate"/>, <paramref name="endDate"/>], restricted to <paramref name="includedRoots"/>.
    /// A date/Root combination with no hits is simply absent, not a zero-valued row.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="includedRoots">The Roots to restrict results to.</param>
    /// <returns>The matching daily totals, ordered by date then Root.</returns>
    public IEnumerable<ByRootDailyTotalRow> GetDailyTotals(DateOnly startDate, DateOnly endDate, IReadOnlyCollection<string> includedRoots)
    {
        ArgumentNullException.ThrowIfNull(includedRoots);

        return Connection.Query<ByRootDailyTotalRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, IncludedRoots = includedRoots });
    }
}
