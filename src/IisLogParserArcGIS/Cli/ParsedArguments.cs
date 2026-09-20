namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The three command-line arguments after validation and normalization.
/// </summary>
/// <param name="LogSourceDirectory">The directory containing the IIS W3C log files to process.</param>
/// <param name="TargetLocalDate">The target local date to process.</param>
/// <param name="OutputDatabasePath">The path to the output SQLite database file.</param>
public sealed record ParsedArguments(string LogSourceDirectory, DateOnly TargetLocalDate, string OutputDatabasePath);
