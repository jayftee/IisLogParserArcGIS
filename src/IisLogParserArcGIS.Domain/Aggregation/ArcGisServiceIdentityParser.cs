using System.Diagnostics.CodeAnalysis;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Parses the (site, folder, service_name, service_type) identity of a genuine ArcGIS Server REST service call
/// from a <c>cs-uri-stem</c> value, per ticket 12. Admin traffic (<c>/site/admin/*</c>) and Portal traffic
/// (<c>/portal/*</c>) are never a service call. <c>site</c> reuses <see cref="RootNormalizer"/> per ticket 07.
/// </summary>
public static class ArcGisServiceIdentityParser
{
    private const string RestSegment = "rest";
    private const string ServicesSegment = "services";
    private const string AdminSegment = "admin";
    private const string PortalSegment = "portal";
    private const string ServiceTypeSuffix = "Server";

#pragma warning disable CA1054 // A raw cs-uri-stem log field value, not a well-formed System.Uri.
    /// <summary>
    /// Attempts to parse <paramref name="uriStem"/> as a genuine ArcGIS Server REST service call of the shape
    /// <c>/site/rest/services/[folder/]service_name/service_type/...</c>. The trailing operation segment
    /// (<c>export</c>, <c>query</c>, <c>applyEdits</c>, …) is ignored. A path that merely contains "rest"
    /// somewhere, without matching this exact segment shape, does not parse.
    /// </summary>
    /// <param name="uriStem">The raw <c>cs-uri-stem</c> field value.</param>
    /// <param name="identity">The parsed identity, when parsing succeeds; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="uriStem"/> is a genuine service call.</returns>
    public static bool TryParse(string uriStem, [NotNullWhen(true)] out ArcGisServiceIdentity? identity)
#pragma warning restore CA1054
    {
        ArgumentNullException.ThrowIfNull(uriStem);

        identity = null;

        var segments = uriStem.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || IsPortalTraffic(segments) || IsAdminTraffic(segments))
        {
            return false;
        }

        if (HasServicesPathShape(segments))
        {
            return TryParseServiceIdentity(segments[3..], RootNormalizer.Normalize(uriStem), out identity);
        }

        return false;
    }

    /// <summary>
    /// Matches <paramref name="remainingSegments"/> (everything after <c>services</c>) against the foldered shape
    /// (folder/name/type) before the folderless shape (name/type/operation), since both can have 3 segments and
    /// only a foldered path's 3rd segment is itself type-shaped.
    /// </summary>
    private static bool TryParseServiceIdentity(string[] remainingSegments, string site, [NotNullWhen(true)] out ArcGisServiceIdentity? identity)
    {
        if (remainingSegments.Length >= 3 && IsServiceTypeSegment(remainingSegments[2]))
        {
            identity = new ArcGisServiceIdentity
            {
                Site = site,
                Folder = Lowercase(remainingSegments[0]),
                ServiceName = Lowercase(remainingSegments[1]),
                ServiceType = Lowercase(remainingSegments[2]),
            };
            return true;
        }

        if (remainingSegments.Length >= 2 && IsServiceTypeSegment(remainingSegments[1]))
        {
            identity = new ArcGisServiceIdentity
            {
                Site = site,
                Folder = null,
                ServiceName = Lowercase(remainingSegments[0]),
                ServiceType = Lowercase(remainingSegments[1]),
            };
            return true;
        }

        identity = null;
        return false;
    }

#pragma warning disable CA1308 // Lowercasing is the normalized storage form required for case-insensitive grouping, not a security comparison.
    private static string Lowercase(string value) => value.ToLowerInvariant();
#pragma warning restore CA1308

    private static bool HasServicesPathShape(string[] segments) =>
        segments.Length >= 3
        && string.Equals(segments[1], RestSegment, StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[2], ServicesSegment, StringComparison.OrdinalIgnoreCase);

    private static bool IsPortalTraffic(string[] segments) =>
        string.Equals(segments[0], PortalSegment, StringComparison.OrdinalIgnoreCase);

    private static bool IsAdminTraffic(string[] segments) =>
        segments.Length >= 2 && string.Equals(segments[1], AdminSegment, StringComparison.OrdinalIgnoreCase);

    private static bool IsServiceTypeSegment(string segment) =>
        segment.EndsWith(ServiceTypeSuffix, StringComparison.OrdinalIgnoreCase);
}
