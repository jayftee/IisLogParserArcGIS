namespace IisLogParserArcGIS.FileDiscovery;

/// <summary>
/// Resolves a configured, <see cref="TimeZoneInfo"/>-compatible local time zone identifier to its UTC offset on a
/// given local date.
/// </summary>
public static class LocalUtcOffsetResolver
{
    /// <summary>
    /// Resolves <paramref name="timeZoneId"/>'s UTC offset at local midnight on <paramref name="localDate"/>.
    /// </summary>
    /// <param name="timeZoneId">A <see cref="TimeZoneInfo"/>-compatible time zone identifier.</param>
    /// <param name="localDate">The local calendar date to resolve the offset for.</param>
    /// <returns>The time zone's UTC offset at local midnight on <paramref name="localDate"/>.</returns>
    /// <exception cref="TimeZoneNotFoundException">No time zone with the given identifier exists.</exception>
    /// <exception cref="InvalidTimeZoneException">The registry or dataset backing the time zone is corrupt.</exception>
    public static TimeSpan Resolve(string timeZoneId, DateOnly localDate)
    {
        ArgumentNullException.ThrowIfNull(timeZoneId);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localMidnight = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        return timeZone.GetUtcOffset(localMidnight);
    }
}
