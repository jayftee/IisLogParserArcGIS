namespace IisLogParserArcGIS.Domain.Parsing;

/// <summary>
/// The IIS W3C field names, as they appear in a <c>#Fields</c> header line, that the aggregate computations rely
/// on unconditionally. A header block missing any field in <see cref="All"/> causes its whole physical file to be
/// skipped (see <see cref="LogFileParser"/>); <see cref="ForwardedFor"/> is resolved separately because it is
/// optional.
/// </summary>
public static class RequiredLogFieldNames
{
    /// <summary>The field carrying the UTC calendar date component of a log line's timestamp.</summary>
    public const string Date = "date";

    /// <summary>The field carrying the UTC time-of-day component of a log line's timestamp.</summary>
    public const string Time = "time";

    /// <summary>The field carrying the requested URI stem.</summary>
    public const string UriStem = "cs-uri-stem";

    /// <summary>The field carrying the client's user agent string.</summary>
    public const string UserAgent = "cs(User-Agent)";

    /// <summary>The field carrying the client's referer string.</summary>
    public const string Referer = "cs(Referer)";

    /// <summary>The field carrying the HTTP status code returned to the client.</summary>
    public const string Status = "sc-status";

    /// <summary>The field carrying the time taken to serve the request, in milliseconds.</summary>
    public const string TimeTaken = "time-taken";

    /// <summary>The field carrying the forwarded-for client IP address list.</summary>
    public const string ForwardedFor = "X-Forwarded-For";

    /// <summary>
    /// All required field names. <see cref="ForwardedFor"/> is deliberately excluded - IIS only logs it when an
    /// operator opts in, so a header block omitting it is still valid (see <see cref="LogFileParser"/>).
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        Date, Time, UriStem, UserAgent, Referer, Status, TimeTaken,
    ];
}
