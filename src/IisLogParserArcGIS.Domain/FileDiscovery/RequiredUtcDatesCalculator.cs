namespace IisLogParserArcGIS.Domain.FileDiscovery;

/// <summary>
/// Computes which UTC calendar date(s) of raw IIS log files are needed to fully cover a given local calendar day.
/// </summary>
public static class RequiredUtcDatesCalculator
{
    /// <summary>
    /// Computes the UTC calendar date(s) needed to fully cover <paramref name="targetLocalDate"/>, given the
    /// configured local time zone's UTC offset.
    /// </summary>
    /// <param name="targetLocalDate">The local calendar date being processed.</param>
    /// <param name="localUtcOffset">
    /// The configured local time zone's offset from UTC on <paramref name="targetLocalDate"/>. A negative offset
    /// (local time behind UTC) needs the UTC-adjacent date after <paramref name="targetLocalDate"/>; a positive
    /// offset (local time ahead of UTC) needs the UTC-adjacent date before it; a zero offset needs only
    /// <paramref name="targetLocalDate"/> itself.
    /// </param>
    /// <returns>The UTC calendar date(s) needed, in ascending order.</returns>
    public static IReadOnlyList<DateOnly> Calculate(DateOnly targetLocalDate, TimeSpan localUtcOffset)
    {
        if (localUtcOffset < TimeSpan.Zero)
        {
            return new[] { targetLocalDate, targetLocalDate.AddDays(1) };
        }

        if (localUtcOffset > TimeSpan.Zero)
        {
            return new[] { targetLocalDate.AddDays(-1), targetLocalDate };
        }

        return new[] { targetLocalDate };
    }
}
