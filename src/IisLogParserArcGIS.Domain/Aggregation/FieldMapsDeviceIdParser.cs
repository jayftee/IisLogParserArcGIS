using System.Diagnostics.CodeAnalysis;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Parses the ArcGIS Field Maps device id out of a <c>cs(User-Agent)</c> value, per ticket 20. The app appends
/// its per-install GUID in parentheses at the very end of the user-agent, so the id is read from the end of the
/// string and nothing before that trailing group (runtime token, platform group, device model) is examined.
/// </summary>
public static class FieldMapsDeviceIdParser
{
    private const string ProductToken = "arcgis-fieldmaps";

    /// <summary>
    /// Attempts to read the device id from <paramref name="userAgent"/>.
    /// </summary>
    /// <param name="userAgent">The raw <c>cs(User-Agent)</c> field value.</param>
    /// <param name="deviceId">The casefolded (lowercase) device GUID when parsing succeeds; otherwise <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="userAgent"/> is a Field Maps user-agent that ends in a
    /// parenthesised, well-formed GUID.
    /// </returns>
    public static bool TryParse(string userAgent, [NotNullWhen(true)] out string? deviceId)
    {
        ArgumentNullException.ThrowIfNull(userAgent);

        deviceId = null;

        var endsInGroup = userAgent.Contains(ProductToken, StringComparison.OrdinalIgnoreCase) && userAgent.EndsWith(')');
        if (endsInGroup && Guid.TryParseExact(userAgent[(userAgent.LastIndexOf('(') + 1)..^1], "D", out var guid))
        {
            deviceId = guid.ToString("D");
            return true;
        }

        return false;
    }
}
