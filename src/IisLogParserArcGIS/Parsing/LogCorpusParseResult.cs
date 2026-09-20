using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Parsing;

/// <summary>
/// The outcome of parsing every discovered physical log file for one run.
/// </summary>
public sealed record LogCorpusParseResult
{
    /// <summary>
    /// Gets the successfully normalized requests across every non-skipped file.
    /// </summary>
    public required IReadOnlyList<NormalizedLogRequest> Requests { get; init; }

    /// <summary>
    /// Gets the combined data-line counts across every non-skipped file.
    /// </summary>
    public required LogLineCounts Counts { get; init; }

    /// <summary>
    /// Gets the number of files skipped entirely because of a header schema problem.
    /// </summary>
    public required int SkippedFileCount { get; init; }
}
