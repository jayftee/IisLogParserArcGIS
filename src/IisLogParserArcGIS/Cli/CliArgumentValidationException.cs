namespace IisLogParserArcGIS.Cli;

/// <summary>
/// Thrown when a positional command-line argument is present but malformed. The <see cref="Exception.Message"/>
/// is written directly to the console as the operator-facing validation error, without a stack trace.
/// </summary>
public sealed class CliArgumentValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliArgumentValidationException"/> class.
    /// </summary>
    public CliArgumentValidationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliArgumentValidationException"/> class with a clear,
    /// operator-facing validation message.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public CliArgumentValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliArgumentValidationException"/> class with a clear
    /// validation message and the underlying cause.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <param name="innerException">The underlying cause of the validation failure.</param>
    public CliArgumentValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
