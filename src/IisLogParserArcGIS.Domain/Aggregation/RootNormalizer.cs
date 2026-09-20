namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Derives the "root" grouping key shared by the by-root aggregate (ticket 07) and the ArcGIS Server service
/// aggregate's <c>site</c> column (ticket 12): the first non-empty path segment of a <c>cs-uri-stem</c> value.
/// </summary>
public static class RootNormalizer
{
    private const int RootMaxLength = 16;
    private const string FallbackRoot = "-";

#pragma warning disable CA1054 // A raw cs-uri-stem log field value, not a well-formed System.Uri.
    /// <summary>
    /// Normalizes <paramref name="uriStem"/> to its root: the first non-empty path segment, lowercased and
    /// truncated to 16 characters, or <c>"-"</c> when no segment is present.
    /// </summary>
    /// <param name="uriStem">The raw <c>cs-uri-stem</c> field value.</param>
    /// <returns>The normalized root.</returns>
    public static string Normalize(string uriStem)
#pragma warning restore CA1054
    {
        ArgumentNullException.ThrowIfNull(uriStem);

        var firstSegment = uriStem.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrEmpty(firstSegment))
        {
            return FallbackRoot;
        }

#pragma warning disable CA1308 // Lowercasing is the normalized storage form required by the spec, not a security comparison.
        var lowered = firstSegment.ToLowerInvariant();
#pragma warning restore CA1308
        return lowered.Length > RootMaxLength ? lowered[..RootMaxLength] : lowered;
    }
}
