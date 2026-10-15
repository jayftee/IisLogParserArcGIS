namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The <c>regenerate</c> verb's command-line arguments after validation and normalization.
/// </summary>
/// <param name="InputDatabasePath">The path to the existing aggregate SQLite database to read.</param>
/// <param name="OutputDirectory">The full path of the directory the Dashboard is generated into.</param>
/// <param name="Year">The calendar year the Dashboard reports on.</param>
public sealed record ParsedRegenerateArguments(string InputDatabasePath, string OutputDirectory, int Year);
