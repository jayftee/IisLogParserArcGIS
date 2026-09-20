namespace IisLogParserArcGIS.Reports.Sections;

/// <summary>
/// Which half of a request outcome split (successful vs. failed, per <c>sc-status &lt; 400</c>) an ArcGIS Server
/// section metric or Leaderboard is scoped to.
/// </summary>
internal enum RequestOutcome
{
    /// <summary>
    /// Requests whose <c>sc-status</c> is less than 400.
    /// </summary>
    Successful,

    /// <summary>
    /// Requests whose <c>sc-status</c> is 400 or greater.
    /// </summary>
    Failed,
}
