namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Derives the "URI" grouping key shared by the by-URI aggregate (ticket 06) and the referer+URI aggregate
/// (ticket 11): the raw <c>cs-uri-stem</c> value truncated to 1024 characters.
/// </summary>
public static class UriStemNormalizer
{
    private const int UriStemMaxLength = 1024;

#pragma warning disable CA1054, CA1056 // A raw cs-uri-stem log field value, not a well-formed System.Uri.
    /// <summary>
    /// Normalizes <paramref name="uriStem"/>: truncated to 1024 characters.
    /// </summary>
    /// <param name="uriStem">The raw <c>cs-uri-stem</c> field value.</param>
    /// <returns>The normalized URI stem.</returns>
    public static string Normalize(string uriStem)
#pragma warning restore CA1054, CA1056
    {
        ArgumentNullException.ThrowIfNull(uriStem);

        return uriStem.Length > UriStemMaxLength ? uriStem[..UriStemMaxLength] : uriStem;
    }
}
