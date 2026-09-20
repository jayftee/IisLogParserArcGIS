using System.Data;
using IisLogParserArcGIS.Data.Queries;

namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Bundles every <c>*Query</c> class <see cref="ArcGisServerSectionBuilder"/> needs, constructed once per
/// Regeneration Run and reused across every Included Root, rather than re-instantiated per site.
/// </summary>
internal sealed class ArcGisServerQueries
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArcGisServerQueries"/> class.
    /// </summary>
    /// <param name="connection">The aggregate database connection every query runs against.</param>
    public ArcGisServerQueries(IDbConnection connection)
    {
        HitTotals = new ByArcGisServiceDailyHitTotalsQuery(connection);
        AverageTimeTotals = new ByArcGisServiceDailyAverageTimeQuery(connection);
        TopSuccessfulHits = new ByArcGisServiceTopSuccessfulHitsQuery(connection);
        TopFailedHits = new ByArcGisServiceTopFailedHitsQuery(connection);
        TopAverageSuccessfulTime = new ByArcGisServiceTopAverageSuccessfulTimeQuery(connection);
        TopAverageFailedTime = new ByArcGisServiceTopAverageFailedTimeQuery(connection);
    }

    /// <summary>
    /// Gets the per-day hit-count totals query, for the successful/failed request evolution charts.
    /// </summary>
    public ByArcGisServiceDailyHitTotalsQuery HitTotals { get; }

    /// <summary>
    /// Gets the per-day average-time totals query, for the average-processing-time evolution charts.
    /// </summary>
    public ByArcGisServiceDailyAverageTimeQuery AverageTimeTotals { get; }

    /// <summary>
    /// Gets the Top-50-by-successful-hits Leaderboard query.
    /// </summary>
    public ByArcGisServiceTopSuccessfulHitsQuery TopSuccessfulHits { get; }

    /// <summary>
    /// Gets the Top-50-by-failed-hits Leaderboard query.
    /// </summary>
    public ByArcGisServiceTopFailedHitsQuery TopFailedHits { get; }

    /// <summary>
    /// Gets the Top-50-by-average-successful-time Leaderboard query.
    /// </summary>
    public ByArcGisServiceTopAverageSuccessfulTimeQuery TopAverageSuccessfulTime { get; }

    /// <summary>
    /// Gets the Top-50-by-average-failed-time Leaderboard query.
    /// </summary>
    public ByArcGisServiceTopAverageFailedTimeQuery TopAverageFailedTime { get; }
}
