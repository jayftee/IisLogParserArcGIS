namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// The independently-computed line counts <see cref="RawCorpusLineCounter"/> produces for one UTC calendar date.
/// </summary>
internal sealed record CorpusDateCounts
{
    /// <summary>
    /// Gets the total count of data lines across every matching, non-skipped file. IIS log files occasionally
    /// carry a handful of lines timestamped for the adjacent calendar date near midnight, so this can exceed
    /// <see cref="MatchingDate"/>.
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// Gets the count of data lines across every matching, non-skipped file whose own raw <c>date</c> field
    /// equals the counted UTC date and whose field count matches its governing header.
    /// </summary>
    public required int MatchingDate { get; init; }
}
