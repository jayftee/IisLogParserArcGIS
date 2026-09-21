using System.Net;
using System.Net.Sockets;

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

    /// <summary>
    /// Normalizes <paramref name="forwardedFor"/>: an absent value (empty or <c>"-"</c>) becomes the loopback
    /// placeholder <c>"127.0.0.1"</c>. Otherwise <c>+</c> is removed, the first comma-separated value is taken and
    /// trimmed, and its client IP is extracted: a bracketed IPv6 address (<c>[addr]</c> or <c>[addr]:port</c>) loses
    /// its brackets and port; a value that is an IP address as a whole (IPv4, or IPv6 with its own colons) is kept
    /// as is; otherwise the IPv4 address before the first colon is taken (<c>ip:port</c>, <c>ip:ip</c>). A value
    /// that yields no valid IP address becomes <c>"-"</c>. The result is truncated to 48 characters.
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
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();

        var ipAddress = firstValue is { Length: > 0 } ? ExtractIpAddress(firstValue) : null;

        return ipAddress is null ? DashPlaceholder : Truncate(ipAddress);
    }

    private static string? ExtractIpAddress(string value)
    {
        if (value[0] == '[')
        {
            return ExtractBracketedIpv6Address(value);
        }

        if (IPAddress.TryParse(value, out _))
        {
            return value;
        }

        var firstGroup = value.Split(':')[0];

        return IsDottedIpv4Address(firstGroup) ? firstGroup : null;
    }

    private static string? ExtractBracketedIpv6Address(string value)
    {
        var closingBracket = value.IndexOf(']', StringComparison.Ordinal);

        if (closingBracket < 0)
        {
            return null;
        }

        var address = value[1..closingBracket];

        return IPAddress.TryParse(address, out var parsed) && parsed.AddressFamily == AddressFamily.InterNetworkV6 ? address : null;
    }

    private static bool IsDottedIpv4Address(string value) =>
        value.Contains('.', StringComparison.Ordinal)
        && IPAddress.TryParse(value, out var parsed)
        && parsed.AddressFamily == AddressFamily.InterNetwork;

    private static string Truncate(string value) =>
        value.Length > ForwardedForIpMaxLength ? value[..ForwardedForIpMaxLength] : value;
}
