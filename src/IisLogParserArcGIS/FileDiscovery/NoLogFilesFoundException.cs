namespace IisLogParserArcGIS.FileDiscovery;

/// <summary>
/// Thrown when no physical log files exist for any of the UTC calendar date(s) needed to cover the target local
/// date. The <see cref="Exception.Message"/> is written directly to the console as the operator-facing error,
/// without a stack trace.
/// </summary>
public sealed class NoLogFilesFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoLogFilesFoundException"/> class.
    /// </summary>
    public NoLogFilesFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoLogFilesFoundException"/> class with a clear,
    /// operator-facing error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NoLogFilesFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoLogFilesFoundException"/> class with a clear error message
    /// and the underlying cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying cause of the failure.</param>
    public NoLogFilesFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
