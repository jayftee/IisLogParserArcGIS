using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads per-Root totals directly from <see cref="AggregateTableNames.ByRoot"/> for the Summary section's
/// Complete View table. The by-Root aggregate's row grain already matches what this needs, so no view is
/// required (per ADR-0002 and ticket 03's resolution).
/// </summary>
public sealed class ByRootRangeTotalsQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT root AS Root, SUM(hits) AS Hits, SUM(time_taken_second) AS TotalTimeTakenSecond
        FROM {AggregateTableNames.ByRoot}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND root IN @IncludedRoots
        GROUP BY root
        ORDER BY root
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByRootRangeTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByRootRangeTotalsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets one row per Root with at least one hit within [<paramref name="startDate"/>, <paramref name="endDate"/>],
    /// restricted to <paramref name="includedRoots"/>. A Root with no hits in range is simply absent, not a
    /// zero-valued row - the caller decides whether to fill in the rest of <paramref name="includedRoots"/>.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="includedRoots">The Roots to restrict results to.</param>
    /// <returns>The matching range totals, ordered by Root.</returns>
    public IEnumerable<ByRootRangeTotalRow> GetRangeTotals(DateOnly startDate, DateOnly endDate, IReadOnlyCollection<string> includedRoots)
    {
        ArgumentNullException.ThrowIfNull(includedRoots);

        return Connection.Query<ByRootRangeTotalRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, IncludedRoots = includedRoots });
    }
}
