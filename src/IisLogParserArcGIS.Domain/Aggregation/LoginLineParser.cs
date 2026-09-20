using System.Diagnostics.CodeAnalysis;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Parses the username out of the <c>cs-uri-stem</c> of an ArcGIS mobile app "login" request (Field Maps, per
/// ticket 20; Survey123, per ticket 21 - the stem is identical, only the HTTP method differs and is not
/// examined). The app only reveals who is signed in when it starts, by requesting the user's own Portal
/// community resource:
/// <c>/&lt;web adaptor&gt;/sharing/rest/community/users/&lt;username&gt;</c>, optionally followed by a
/// sub-resource (e.g. <c>userLicenseType</c>) that is not part of the identity. Matching is segment-based and
/// case-insensitive, like <see cref="PortalItemIdentityParser"/>.
/// </summary>
public static class LoginLineParser
{
    private const int UsernameSegmentIndex = 5;
    private const string SharingSegment = "sharing";
    private const string RestSegment = "rest";
    private const string CommunitySegment = "community";
    private const string UsersSegment = "users";

#pragma warning disable CA1054 // A raw cs-uri-stem log field value, not a well-formed System.Uri.
    /// <summary>
    /// Attempts to parse <paramref name="uriStem"/> as a login request under
    /// <paramref name="portalWebAdaptorName"/>.
    /// </summary>
    /// <param name="uriStem">The raw <c>cs-uri-stem</c> field value.</param>
    /// <param name="portalWebAdaptorName">The configured Portal Web Adaptor name (case-insensitive match).</param>
    /// <param name="username">The username when parsing succeeds; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="uriStem"/> is a login request.</returns>
    public static bool TryParse(string uriStem, string portalWebAdaptorName, [NotNullWhen(true)] out string? username)
#pragma warning restore CA1054
    {
        ArgumentNullException.ThrowIfNull(uriStem);
        ArgumentNullException.ThrowIfNull(portalWebAdaptorName);

        username = null;

        var segments = uriStem.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (HasCommunityUsersPathShape(segments, portalWebAdaptorName))
        {
#pragma warning disable CA1308 // Lowercasing is the normalized storage form for case-insensitive grouping, not a security comparison.
            username = Uri.UnescapeDataString(segments[UsernameSegmentIndex]).ToLowerInvariant();
#pragma warning restore CA1308
            return true;
        }

        return false;
    }

    private static bool HasCommunityUsersPathShape(string[] segments, string portalWebAdaptorName) =>
        segments.Length > UsernameSegmentIndex
        && string.Equals(segments[0], portalWebAdaptorName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[1], SharingSegment, StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[2], RestSegment, StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[3], CommunitySegment, StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[4], UsersSegment, StringComparison.OrdinalIgnoreCase);
}
