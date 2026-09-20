namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Formats one ArcGIS Server service's (folder, service_name, service_type) identity as the single display label
/// used everywhere a service needs a human-readable name but has no dedicated columns to show folder/name/type
/// separately (a Leaderboard bar, a Detail Page's own title) - the Complete View table is the one place these
/// stay as their own columns instead, per ticket 12.
/// </summary>
internal static class ArcGisServiceLabelFormatter
{
    /// <summary>
    /// Formats <paramref name="folder"/>/<paramref name="serviceName"/>/<paramref name="serviceType"/> as one label.
    /// </summary>
    /// <param name="folder">The service's folder, or <see langword="null"/> for a folderless service.</param>
    /// <param name="serviceName">The service's name.</param>
    /// <param name="serviceType">The service's type.</param>
    /// <returns><c>"{serviceName} ({serviceType})"</c>, or <c>"{folder}/{serviceName} ({serviceType})"</c> when <paramref name="folder"/> is not <see langword="null"/>.</returns>
    public static string Format(string? folder, string serviceName, string serviceType)
    {
        return folder is null ? $"{serviceName} ({serviceType})" : $"{folder}/{serviceName} ({serviceType})";
    }
}
