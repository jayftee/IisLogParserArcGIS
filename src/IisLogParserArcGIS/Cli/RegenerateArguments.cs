using CommandLine;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The <c>regenerate</c> verb's raw, unvalidated arguments: performs a Regeneration Run against an existing
/// aggregate database's current contents. Never harvests - no log source directory or target local date.
/// </summary>
[Verb("regenerate", HelpText = "Regenerate the entire Dashboard for one year from an aggregate database's current contents. Does not harvest.")]
public sealed class RegenerateArguments
{
    /// <summary>
    /// Gets the path to the existing aggregate SQLite database to read.
    /// </summary>
    [Value(0, MetaName = "inputDatabasePath", Required = true, HelpText = "Path to the existing aggregate SQLite database to read.")]
    public string InputDatabasePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the directory the Dashboard is generated into.
    /// </summary>
    [Value(1, MetaName = "outputDirectory", Required = true, HelpText = "Directory to generate the Dashboard into: new, empty, or holding a previous Dashboard (.iis-dashboard marker).")]
    public string OutputDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Gets the calendar year the Dashboard reports on.
    /// </summary>
    [Option("year", Required = true, HelpText = "The four-digit calendar year the Dashboard reports on, e.g. 2026.")]
    public int Year { get; init; }
}
