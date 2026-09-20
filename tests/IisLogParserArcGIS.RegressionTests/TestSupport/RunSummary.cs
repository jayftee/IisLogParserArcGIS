namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// The end-of-run summary line an operator reads off stdout, parsed back out of the process's captured output.
/// </summary>
internal sealed record RunSummary
{
    /// <summary>
    /// Gets the reported total line count.
    /// </summary>
    public required int Total { get; init; }

    /// <summary>
    /// Gets the reported valid-line count.
    /// </summary>
    public required int Valid { get; init; }

    /// <summary>
    /// Gets the reported invalid/skipped-line count.
    /// </summary>
    public required int Invalid { get; init; }
}
