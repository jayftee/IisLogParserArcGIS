using System.Net;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Derives the "forwarded-for IP" grouping key for the by-forwarded-for-IP aggregate (ticket 10): the first
/// validated client IP address from a <c>X-Forwarded-For</c> value.
/// </summary>
public static class ForwardedForIpNormalizer
{
    private const int ForwardedForIpMaxLength = 48;
    private const string DashPlaceholder = "-";
    private const string LoopbackPlaceholder = "127.0.0.1";
    private static readonly char[] _valueSeparators = [',', ':'];

    /// <summary>
    /// Normalizes <paramref name="forwardedFor"/>: an absent value (empty or <c>"-"</c>) becomes the loopback
    /// placeholder <c>"127.0.0.1"</c>. Otherwise <c>+</c> is removed, the first comma/colon-separated value is
    /// taken and trimmed, and validated as an IP address; a value that fails validation becomes <c>"-"</c>. The
    /// result is truncated to 48 characters.
    /// </summary>
    /// <param name="forwardedFor">The raw <c>X-Forwarded-For</c> field value.</param>
    /// <returns>The normalized forwarded-for IP.</returns>
    public static string Normalize(string forwardedFor)
    {
        ArgumentNullException.ThrowIfNull(forwardedFor);

        if (forwardedFor.Length == 0 || forwardedFor == DashPlaceholder)
        {
            return LoopbackPlaceholder;
        }

        var firstValue = forwardedFor
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Split(_valueSeparators, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();

        var isValidIpAddress = firstValue is { Length: > 0 } && IPAddress.TryParse(firstValue, out _);

        return isValidIpAddress ? Truncate(firstValue!) : DashPlaceholder;
    }

    private static string Truncate(string value) =>
        value.Length > ForwardedForIpMaxLength ? value[..ForwardedForIpMaxLength] : value;
}
