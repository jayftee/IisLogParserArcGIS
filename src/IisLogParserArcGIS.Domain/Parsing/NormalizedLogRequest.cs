namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// One raw IIS log line, parsed and normalized into the shared shape every aggregate computation consumes.
/// Field-specific normalization (case folding, truncation, IP parsing, ArcGIS service path parsing, and so on) is
/// each aggregate's own responsibility; the values here are otherwise as recorded in the raw log line.
/// </summary>
public sealed record NormalizedLogRequest
{
    /// <summary>
    /// Gets the log line's <c>date</c> and <c>time</c> fields combined, in UTC.
    /// </summary>
    public required DateTime UtcDateTime { get; init; }

    /// <summary>
    /// Gets <see cref="UtcDateTime"/> converted using the configured local time zone.
    /// </summary>
    public required DateTime LocalDateTime { get; init; }

    /// <summary>
    /// Gets the date-only part of <see cref="LocalDateTime"/> - the aggregate grouping key.
    /// </summary>
    public required DateOnly LocalDate { get; init; }

#pragma warning disable CA1054, CA1056 // A raw cs-uri-stem log field value, not a well-formed System.Uri.
    /// <summary>
    /// Gets the raw <c>cs-uri-stem</c> field value.
    /// </summary>
    public required string UriStem { get; init; }
#pragma warning restore CA1054, CA1056

    /// <summary>
    /// Gets the raw <c>cs(User-Agent)</c> field value.
    /// </summary>
    public required string UserAgent { get; init; }

    /// <summary>
    /// Gets the raw <c>cs(Referer)</c> field value.
    /// </summary>
    public required string Referer { get; init; }

    /// <summary>
    /// Gets the parsed <c>sc-status</c> field value.
    /// </summary>
    public required int Status { get; init; }

    /// <summary>
    /// Gets the parsed <c>time-taken</c> field value, in milliseconds as recorded.
    /// </summary>
    public required int TimeTakenMilliseconds { get; init; }

    /// <summary>
    /// Gets the raw <c>X-Forwarded-For</c> field value, or <see langword="null"/> when this line's governing
    /// header never declared the field (it is optional - IIS only logs it when configured to). This is distinct
    /// from an empty or <c>"-"</c> value, which means the header declared it but this line has no value.
    /// </summary>
    public required string? ForwardedFor { get; init; }
}
