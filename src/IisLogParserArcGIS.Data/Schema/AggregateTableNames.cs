namespace IisLogParserArcGIS.Data.Schema;

/// <summary>
/// The single source of truth for the ten aggregate table names, shared by schema creation, repositories,
/// and tests so the same name is never retyped independently in more than one place.
/// </summary>
public static class AggregateTableNames
{
    /// <summary>
    /// Gets the by-URI aggregate table name.
    /// </summary>
    public const string ByUri = "aggregated_by_uri";

    /// <summary>
    /// Gets the by-root aggregate table name.
    /// </summary>
    public const string ByRoot = "aggregated_by_root";

    /// <summary>
    /// Gets the by-user-agent aggregate table name.
    /// </summary>
    public const string ByUserAgent = "aggregated_by_user_agent";

    /// <summary>
    /// Gets the by-referer aggregate table name.
    /// </summary>
    public const string ByReferer = "aggregated_by_referer";

    /// <summary>
    /// Gets the by-forwarded-for-IP aggregate table name.
    /// </summary>
    public const string ByForwardedForIp = "aggregated_by_forwarded_for_ip";

    /// <summary>
    /// Gets the by-referer-and-URI aggregate table name.
    /// </summary>
    public const string ByRefererAndUri = "aggregated_by_referer_and_uri";

    /// <summary>
    /// Gets the by-ArcGIS-Server-service aggregate table name.
    /// </summary>
    public const string ByArcGisService = "aggregated_by_arcgis_service";

    /// <summary>
    /// Gets the by-Portal-item aggregate table name.
    /// </summary>
    public const string ByPortalItem = "aggregated_by_portal_item";

    /// <summary>
    /// Gets the by-Field-Maps-device aggregate table name.
    /// </summary>
#pragma warning disable CC0309 // "Field Maps" is the Esri product name, not a log field.
    public const string ByFieldMapsDevice = "aggregated_by_field_maps_device";
#pragma warning restore CC0309

    /// <summary>
    /// Gets the by-Survey123-device aggregate table name.
    /// </summary>
    public const string BySurvey123Device = "aggregated_by_survey123_device";

    /// <summary>
    /// Gets every aggregate table name.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ByUri,
        ByRoot,
        ByUserAgent,
        ByReferer,
        ByForwardedForIp,
        ByRefererAndUri,
        ByArcGisService,
        ByPortalItem,
        ByFieldMapsDevice,
        BySurvey123Device,
    ];
}
