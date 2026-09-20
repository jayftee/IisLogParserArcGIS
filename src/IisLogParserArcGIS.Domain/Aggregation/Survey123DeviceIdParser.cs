using System.Diagnostics.CodeAnalysis;

namespace IisLogParserArcGIS.Domain.Aggregation;

/// <summary>
/// Parses the ArcGIS Survey123 device id out of a <c>cs(User-Agent)</c> value, per ticket 21. Unlike Field Maps,
/// Survey123 does not end its user-agent with the id: it is the last <c>;+</c>-separated item of the first
/// parenthesised group after <c>AppFramework/&lt;version&gt;+</c> (the platform group), as 32 hex digits with no
/// hyphens, and the rest of the string follows it.
/// </summary>
public static class Survey123DeviceIdParser
{
    private const string FrameworkToken = "AppFramework/";
    private const string ProductToken = "Survey123";
    private const string ItemSeparator = ";+";

    /// <summary>
    /// Attempts to read the device id from <paramref name="userAgent"/>.
    /// </summary>
    /// <param name="userAgent">The raw <c>cs(User-Agent)</c> field value.</param>
    /// <param name="deviceId">The casefolded (lowercase) 32-hex device id when parsing succeeds; otherwise <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="userAgent"/> carries both <c>AppFramework</c> and
    /// <c>Survey123</c> and the last item of its platform group is a well-formed 32-hex id.
    /// </returns>
    public static bool TryParse(string userAgent, [NotNullWhen(true)] out string? deviceId)
    {
        ArgumentNullException.ThrowIfNull(userAgent);

        deviceId = null;

        var frameworkIndex = userAgent.IndexOf(FrameworkToken, StringComparison.OrdinalIgnoreCase);
        var isSurvey123 = frameworkIndex >= 0 && userAgent.Contains(ProductToken, StringComparison.OrdinalIgnoreCase);
        if (isSurvey123)
        {
            deviceId = ReadPlatformGroupDeviceId(userAgent, frameworkIndex + FrameworkToken.Length);
        }

        return deviceId is not null;
    }

    private static string? ReadPlatformGroupDeviceId(string userAgent, int searchStart)
    {
        var groupStart = userAgent.IndexOf('(', searchStart);
        var groupEnd = groupStart < 0 ? -1 : userAgent.IndexOf(')', groupStart);
        if (groupEnd < 0)
        {
            return null;
        }

        var platformGroup = userAgent[(groupStart + 1)..groupEnd];
        var separatorIndex = platformGroup.LastIndexOf(ItemSeparator, StringComparison.Ordinal);
        var candidate = separatorIndex < 0 ? platformGroup : platformGroup[(separatorIndex + ItemSeparator.Length)..];

        return Guid.TryParseExact(candidate, "N", out var guid) ? guid.ToString("N") : null;
    }
}
