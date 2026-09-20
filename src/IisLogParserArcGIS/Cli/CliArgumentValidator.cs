using System.Globalization;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// Validates and normalizes the raw per-verb arguments (<see cref="IHarvestArguments"/> or
/// <see cref="RegenerateArguments"/>) parsed from the command line.
/// </summary>
public static class CliArgumentValidator
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Validates the raw arguments of a Harvest Run verb (<c>harvest</c> or <c>harvest-regenerate</c>), returning
    /// their normalized, typed form.
    /// </summary>
    /// <param name="arguments">The raw arguments parsed from the command line.</param>
    /// <returns>The validated, normalized arguments.</returns>
    /// <exception cref="CliArgumentValidationException">A required argument is malformed.</exception>
    public static ParsedArguments Validate(IHarvestArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var logSourceDirectory = ValidatePath(arguments.LogSourceDirectory, "log source directory");
        var targetLocalDate = ValidateDate(arguments.TargetLocalDate);
        var outputDatabasePath = ValidatePath(arguments.OutputDatabasePath, "output database path");

        return new ParsedArguments(logSourceDirectory, targetLocalDate, outputDatabasePath);
    }

    /// <summary>
    /// Validates the raw argument of the <c>regenerate</c> verb, returning its normalized, typed form.
    /// </summary>
    /// <param name="arguments">The raw argument parsed from the command line.</param>
    /// <returns>The validated, normalized argument.</returns>
    /// <exception cref="CliArgumentValidationException">
    /// The argument is malformed, or names a database that does not exist.
    /// </exception>
    public static ParsedRegenerateArguments Validate(RegenerateArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var inputDatabasePath = ValidatePath(arguments.InputDatabasePath, "input database path");

        if (File.Exists(inputDatabasePath) is false)
        {
            throw new CliArgumentValidationException(
                $"The input database path '{inputDatabasePath}' does not exist. The regenerate verb reads an existing aggregate database - it never creates one.");
        }

        return new ParsedRegenerateArguments(inputDatabasePath);
    }

    private static string ValidatePath(string value, string argumentDescription)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CliArgumentValidationException($"The {argumentDescription} must not be empty.");
        }

        try
        {
            return Path.GetFullPath(value);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new CliArgumentValidationException($"The {argumentDescription} '{value}' is not a valid path.", ex);
        }
    }

    private static DateOnly ValidateDate(string value)
    {
        if (DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetLocalDate))
        {
            return targetLocalDate;
        }

        throw new CliArgumentValidationException($"The target local date '{value}' is not valid. Expected format: {DateFormat}.");
    }
}
