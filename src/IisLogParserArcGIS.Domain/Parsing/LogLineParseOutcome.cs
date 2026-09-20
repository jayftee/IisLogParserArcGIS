namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// The outcome of attempting to parse one raw log data line with <see cref="LogLineParser"/>.
/// </summary>
public sealed record LogLineParseOutcome
{
    /// <summary>
    /// Gets the normalized request, when parsing succeeded.
    /// </summary>
    public NormalizedLogRequest? Request { get; init; }

    /// <summary>
    /// Gets the human-readable reason parsing failed, when it did.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Gets a value indicating whether the line was successfully parsed.
    /// </summary>
    public bool Succeeded => Request is not null;

    /// <summary>
    /// Creates a successful outcome.
    /// </summary>
    /// <param name="request">The normalized request.</param>
    /// <returns>The successful outcome.</returns>
    public static LogLineParseOutcome Success(NormalizedLogRequest request) => new() { Request = request };

    /// <summary>
    /// Creates a failed outcome.
    /// </summary>
    /// <param name="failureReason">The human-readable reason parsing failed.</param>
    /// <returns>The failed outcome.</returns>
    public static LogLineParseOutcome Failure(string failureReason) => new() { FailureReason = failureReason };
}
