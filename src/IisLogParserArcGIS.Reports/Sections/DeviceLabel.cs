namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// How a per-device section (Fieldmaps, Survey123) names one device to the operator (tickets 21, 22): by username when the
/// device is attributed, always followed by the complete device id - a user with two devices appears twice, so
/// the id is what keeps the two bars distinguishable.
/// </summary>
internal static class DeviceLabel
{
    /// <summary>
    /// Formats a device's Leaderboard bar label: <c>username · device-id</c> when attributed, or the complete
    /// device id alone when not.
    /// </summary>
    /// <param name="username">The device's username, or <see langword="null"/> when it is unattributed.</param>
    /// <param name="deviceId">The complete, casefolded device id.</param>
    /// <returns>The label.</returns>
    public static string Format(string? username, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);

        return username is null ? deviceId : $"{username} · {deviceId}";
    }
}
