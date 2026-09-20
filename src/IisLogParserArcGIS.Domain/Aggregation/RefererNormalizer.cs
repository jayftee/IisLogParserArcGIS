namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Derives the "referer" grouping key shared by the by-referer aggregate (ticket 09) and the referer+URI
/// aggregate (ticket 11): the normalized <c>cs(Referer)</c> value.
/// </summary>
public static class RefererNormalizer
{
    private const int RefererMaxLength = 4096;
    private const string EmptyPlaceholder = "-";

    /// <summary>
    /// Normalizes <paramref name="referer"/>: empty or <c>"-"</c> becomes <c>"-"</c>; any other value (including
    /// an opaque non-URL value such as an internal Java class name, which is accepted as-is) has <c>+</c>
    /// decoded to a space, is lowercased, trimmed, and truncated to 4096 characters.
    /// </summary>
    /// <param name="referer">The raw <c>cs(Referer)</c> field value.</param>
    /// <returns>The normalized referer.</returns>
    public static string Normalize(string referer)
    {
        ArgumentNullException.ThrowIfNull(referer);

        if (referer.Length == 0 || referer == EmptyPlaceholder)
        {
            return EmptyPlaceholder;
        }

#pragma warning disable CA1308 // Lowercasing is the normalized storage form required by the spec, not a security comparison.
        var normalized = referer.Replace('+', ' ').ToLowerInvariant().Trim();
#pragma warning restore CA1308
        if (normalized.Length == 0)
        {
            return EmptyPlaceholder;
        }

        return normalized.Length > RefererMaxLength ? normalized[..RefererMaxLength] : normalized;
    }
}
