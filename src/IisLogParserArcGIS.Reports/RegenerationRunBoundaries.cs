namespace IisLogParserArcGIS.Reports;

/// <summary>
/// The year-to-date and Last-7-Days boundaries a Regeneration Run resolves once at startup and threads
/// through every query it makes, so a single run can't straddle a date-boundary tick and disagree with itself
/// across pages. The year is an explicit input: the clock only decides how far into that year the run reaches.
/// </summary>
/// <param name="Today">
/// The last local calendar date every "All" range covers: the current local calendar date, per the configured
/// local time zone, or December 31 of the requested year when that year is already over.
/// </param>
/// <param name="YearStart">The first local calendar date of the requested year (always January 1).</param>
/// <param name="Last7DaysStart">
/// The start of the most recent 7 calendar days, inclusive of the current local calendar date - every section's
/// "Last 7 Days" range is [<see cref="Last7DaysStart"/>, <see cref="Today"/>]. For a year that is already over
/// this lies after <see cref="Today"/>, so the range is empty.
/// </param>
public readonly record struct RegenerationRunBoundaries(DateOnly Today, DateOnly YearStart, DateOnly Last7DaysStart)
{
    private const int Last7DaysWindowLengthInDays = 7;

    /// <summary>
    /// Resolves <see cref="Today"/>, <see cref="YearStart"/>, and <see cref="Last7DaysStart"/> for
    /// <paramref name="year"/> from <paramref name="timeProvider"/>'s current instant, converted to local time via
    /// <paramref name="localTimeZoneId"/>.
    /// </summary>
    /// <param name="timeProvider">The clock abstraction to read the current instant from.</param>
    /// <param name="localTimeZoneId">A <see cref="TimeZoneInfo"/>-compatible local time zone identifier.</param>
    /// <param name="year">The calendar year the run reports on.</param>
    /// <returns>The resolved boundaries.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="year"/> is after the current local calendar year.</exception>
    /// <exception cref="TimeZoneNotFoundException">No time zone with the given identifier exists.</exception>
    /// <exception cref="InvalidTimeZoneException">The registry or dataset backing the time zone is corrupt.</exception>
    public static RegenerationRunBoundaries Resolve(TimeProvider timeProvider, string localTimeZoneId, int year)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(localTimeZoneId);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(localTimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, timeZone);
        var localToday = DateOnly.FromDateTime(localNow);

        if (year > localToday.Year)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year), year, $"The year {year} is after the current local year {localToday.Year}; there is nothing to report yet.");
        }

        var yearEnd = new DateOnly(year, 12, 31);
        var last7DaysStart = localToday.AddDays(-(Last7DaysWindowLengthInDays - 1));

        return new RegenerationRunBoundaries(localToday < yearEnd ? localToday : yearEnd, new DateOnly(year, 1, 1), last7DaysStart);
    }
}
