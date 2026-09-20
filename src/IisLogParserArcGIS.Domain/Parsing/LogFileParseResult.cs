namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// The outcome of parsing one physical log file's lines with <see cref="LogFileParser"/>.
/// </summary>
public sealed record LogFileParseResult
{
    /// <summary>
    /// Gets the successfully normalized requests from this file. Empty when <see cref="WasSkipped"/> is <see langword="true"/>.
    /// </summary>
    public required IReadOnlyList<NormalizedLogRequest> Requests { get; init; }

    /// <summary>
    /// Gets the data-line counts for this file. Zero when <see cref="WasSkipped"/> is <see langword="true"/>.
    /// </summary>
    public required LogLineCounts Counts { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entire file was skipped because one of its header blocks was missing a
    /// required field, or no header block was found at all.
    /// </summary>
    public bool WasSkipped { get; init; }

    /// <summary>
    /// Gets the reason the file was skipped, when <see cref="WasSkipped"/> is <see langword="true"/>.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// Creates a result for a file that was skipped entirely before any line was parsed.
    /// </summary>
    /// <param name="skipReason">The human-readable reason the file was skipped.</param>
    /// <returns>The skipped result.</returns>
    public static LogFileParseResult Skipped(string skipReason) => new()
    {
        Requests = [],
        Counts = LogLineCounts.Zero,
        WasSkipped = true,
        SkipReason = skipReason,
    };

    /// <summary>
    /// Creates a result for a file whose lines were parsed.
    /// </summary>
    /// <param name="requests">The successfully normalized requests.</param>
    /// <param name="counts">The data-line counts.</param>
    /// <returns>The parsed result.</returns>
    public static LogFileParseResult Parsed(IReadOnlyList<NormalizedLogRequest> requests, LogLineCounts counts) => new()
    {
        Requests = requests,
        Counts = counts,
    };
}
