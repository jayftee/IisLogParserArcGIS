using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Replacement;

/// <summary>
/// The freshly computed rows for one local date across all ten aggregate tables, as the unit
/// <see cref="DailyAggregateReplacer.Replace"/> persists atomically.
/// </summary>
public sealed record DailyAggregateBatch
{
    /// <summary>
    /// Gets the local date this batch was aggregated for.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

    /// <summary>
    /// Gets the by-URI aggregate rows.
    /// </summary>
    public required IEnumerable<ByUriAggregateRow> ByUri { get; init; }

    /// <summary>
    /// Gets the by-root aggregate rows.
    /// </summary>
    public required IEnumerable<ByRootAggregateRow> ByRoot { get; init; }

    /// <summary>
    /// Gets the by-user-agent aggregate rows.
    /// </summary>
    public required IEnumerable<ByUserAgentAggregateRow> ByUserAgent { get; init; }

    /// <summary>
    /// Gets the by-referer aggregate rows.
    /// </summary>
    public required IEnumerable<ByRefererAggregateRow> ByReferer { get; init; }

    /// <summary>
    /// Gets the by-forwarded-for-IP aggregate rows.
    /// </summary>
    public required IEnumerable<ByForwardedForIpAggregateRow> ByForwardedForIp { get; init; }

    /// <summary>
    /// Gets the by-referer-and-URI aggregate rows.
    /// </summary>
    public required IEnumerable<ByRefererAndUriAggregateRow> ByRefererAndUri { get; init; }

    /// <summary>
    /// Gets the by-ArcGIS-Server-service aggregate rows.
    /// </summary>
    public required IEnumerable<ByArcGisServiceAggregateRow> ByArcGisService { get; init; }

    /// <summary>
    /// Gets the by-Portal-item aggregate rows.
    /// </summary>
    public required IEnumerable<ByPortalItemAggregateRow> ByPortalItem { get; init; }

    /// <summary>
    /// Gets the by-Field-Maps-device aggregate rows.
    /// </summary>
    public required IEnumerable<ByFieldMapsDeviceAggregateRow> ByFieldMapsDevice { get; init; }

    /// <summary>
    /// Gets the by-Survey123-device aggregate rows.
    /// </summary>
    public required IEnumerable<BySurvey123DeviceAggregateRow> BySurvey123Device { get; init; }
}
