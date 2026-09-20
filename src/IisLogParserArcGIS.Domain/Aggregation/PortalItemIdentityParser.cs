using System.Diagnostics.CodeAnalysis;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Parses an ArcGIS Portal item's identity (its opaque item id) from a <c>cs-uri-stem</c> value, per ticket 18.
/// A request is a portal item request when its path is shaped like one of the Portal Sharing REST API's
/// item-access endpoints under the configured web adaptor name: direct (<c>content/items/&lt;id&gt;</c>),
/// user-scoped (<c>content/users/&lt;username&gt;/items/&lt;id&gt;</c>), or user+folder-scoped
/// (<c>content/users/&lt;username&gt;/&lt;folderId&gt;/items/&lt;id&gt;</c>). The username, folder id, and any
/// trailing sub-resource/operation segment are read only to locate <c>items</c>/<c>&lt;id&gt;</c> and are
/// discarded - never part of the identity. A user-scoped bulk operation with no item id in the path (e.g.
/// <c>deleteItems</c>, <c>shareItems</c>) does not match.
/// </summary>
public static class PortalItemIdentityParser
{
    private const int PortalItemIdMaxLength = 64;
    private const string SharingSegment = "sharing";
    private const string RestSegment = "rest";
    private const string ContentSegment = "content";
    private const string ItemsSegment = "items";
    private const string UsersSegment = "users";

#pragma warning disable CA1054 // A raw cs-uri-stem log field value, not a well-formed System.Uri.
    /// <summary>
    /// Attempts to parse <paramref name="uriStem"/> as an ArcGIS Portal item-access request under
    /// <paramref name="portalWebAdaptorName"/>.
    /// </summary>
    /// <param name="uriStem">The raw <c>cs-uri-stem</c> field value.</param>
    /// <param name="portalWebAdaptorName">The configured Portal Web Adaptor name (case-insensitive match).</param>
    /// <param name="portalItemId">
    /// The parsed, opaque item id, truncated to 64 characters, when parsing succeeds; otherwise
    /// <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> when <paramref name="uriStem"/> is a portal item-access request.</returns>
    public static bool TryParse(string uriStem, string portalWebAdaptorName, [NotNullWhen(true)] out string? portalItemId)
#pragma warning restore CA1054
    {
        ArgumentNullException.ThrowIfNull(uriStem);
        ArgumentNullException.ThrowIfNull(portalWebAdaptorName);

        portalItemId = null;

        var segments = uriStem.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (HasSharingContentPathShape(segments, portalWebAdaptorName))
        {
            return TryParseItemId(segments[4..], out portalItemId);
        }

        return false;
    }

    private static bool TryParseItemId(string[] remainingSegments, [NotNullWhen(true)] out string? portalItemId)
    {
        portalItemId = null;

        if (remainingSegments.Length >= 2 && string.Equals(remainingSegments[0], ItemsSegment, StringComparison.OrdinalIgnoreCase))
        {
            portalItemId = Truncate(remainingSegments[1]);
            return true;
        }

        if (remainingSegments.Length >= 4 && string.Equals(remainingSegments[0], UsersSegment, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(remainingSegments[2], ItemsSegment, StringComparison.OrdinalIgnoreCase))
            {
                portalItemId = Truncate(remainingSegments[3]);
                return true;
            }

            if (remainingSegments.Length >= 5 && string.Equals(remainingSegments[3], ItemsSegment, StringComparison.OrdinalIgnoreCase))
            {
                portalItemId = Truncate(remainingSegments[4]);
                return true;
            }
        }

        return false;
    }

    private static bool HasSharingContentPathShape(string[] segments, string portalWebAdaptorName) =>
        segments.Length >= 5
        && string.Equals(segments[0], portalWebAdaptorName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[1], SharingSegment, StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[2], RestSegment, StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[3], ContentSegment, StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string value) =>
        value.Length > PortalItemIdMaxLength ? value[..PortalItemIdMaxLength] : value;
}
