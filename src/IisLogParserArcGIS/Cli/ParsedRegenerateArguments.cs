namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The <c>regenerate</c> verb's single command-line argument after validation and normalization.
/// </summary>
/// <param name="InputDatabasePath">The path to the existing aggregate SQLite database to read.</param>
public sealed record ParsedRegenerateArguments(string InputDatabasePath);
