namespace IisLogParserArcGIS.Reports;

/// <summary>
/// The calendar-year-to-date and Last-7-Days boundaries a Regeneration Run resolves once at startup and threads
/// through every query it makes, so a single run can't straddle a date-boundary tick and disagree with itself
/// across pages.
/// </summary>
/// <param name="Today">The current local calendar date, per the configured local time zone.</param>
/// <param name="YearStart">The first local calendar date of <see cref="Today"/>'s year (always January 1).</param>
/// <param name="Last7DaysStart">
/// The start of the most recent 7 calendar days, inclusive of <see cref="Today"/> - every section's "Last 7
/// Days" range is [<see cref="Last7DaysStart"/>, <see cref="Today"/>].
/// </param>
public readonly record struct RegenerationRunBoundaries(DateOnly Today, DateOnly YearStart, DateOnly Last7DaysStart)
{
    private const int Last7DaysWindowLengthInDays = 7;

    /// <summary>
    /// Resolves <see cref="Today"/>, <see cref="YearStart"/>, and <see cref="Last7DaysStart"/> from
    /// <paramref name="timeProvider"/>'s current instant, converted to local time via
    /// <paramref name="localTimeZoneId"/>.
    /// </summary>
    /// <param name="timeProvider">The clock abstraction to read the current instant from.</param>
    /// <param name="localTimeZoneId">A <see cref="TimeZoneInfo"/>-compatible local time zone identifier.</param>
    /// <returns>The resolved boundaries.</returns>
    /// <exception cref="TimeZoneNotFoundException">No time zone with the given identifier exists.</exception>
    /// <exception cref="InvalidTimeZoneException">The registry or dataset backing the time zone is corrupt.</exception>
    public static RegenerationRunBoundaries Resolve(TimeProvider timeProvider, string localTimeZoneId)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(localTimeZoneId);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(localTimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, timeZone);
        var today = DateOnly.FromDateTime(localNow);
        var last7DaysStart = today.AddDays(-(Last7DaysWindowLengthInDays - 1));

        return new RegenerationRunBoundaries(today, new DateOnly(today.Year, 1, 1), last7DaysStart);
    }
}
