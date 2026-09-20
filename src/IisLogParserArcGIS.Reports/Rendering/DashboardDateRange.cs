namespace IisLogParserArcGIS.Reports.Rendering;

/// <summary>
/// Which of the Dashboard's two range variants a page shows - per ticket 01, "All" and "Last 7 Days" are always
/// separate static pages, never an in-page toggle (outside Detail Pages).
/// </summary>
public enum DashboardDateRange
{
    /// <summary>
    /// The full calendar-year-to-date range.
    /// </summary>
    All,

    /// <summary>
    /// The most recent 7 calendar days, inclusive of today.
    /// </summary>
    Last7Days,
}
