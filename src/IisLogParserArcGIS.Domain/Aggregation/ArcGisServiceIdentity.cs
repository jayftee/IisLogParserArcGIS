namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// The identity of one ArcGIS Server service: the (site, folder, service_name, service_type) tuple parsed from a
/// <c>cs-uri-stem</c> value by <see cref="ArcGisServiceIdentityParser"/>, per ticket 12.
/// </summary>
public sealed record ArcGisServiceIdentity
{
    /// <summary>
    /// Gets the site: the <see cref="RootNormalizer"/>-normalized first path segment.
    /// </summary>
    public required string Site { get; init; }

    /// <summary>
    /// Gets the lowercased service folder, or <see langword="null"/> for a folderless service.
    /// </summary>
    public string? Folder { get; init; }

    /// <summary>
    /// Gets the lowercased service name.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the lowercased service type, e.g. <c>mapserver</c> or <c>featureserver</c>. Lowercased, like
    /// <see cref="Folder"/> and <see cref="ServiceName"/>, because ArcGIS Server treats these path segments as
    /// case-insensitive and the same service is observed under inconsistent casing across requests.
    /// </summary>
    public required string ServiceType { get; init; }
}
