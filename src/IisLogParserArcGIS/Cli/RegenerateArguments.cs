using CommandLine;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The <c>regenerate</c> verb's raw, unvalidated argument: performs a Regeneration Run against an existing
/// aggregate database's current contents. Never harvests - no log source directory or target local date.
/// </summary>
[Verb("regenerate", HelpText = "Regenerate the entire Dashboard from an aggregate database's current contents. Does not harvest.")]
public sealed class RegenerateArguments
{
    /// <summary>
    /// Gets the path to the existing aggregate SQLite database to read.
    /// </summary>
    [Value(0, MetaName = "inputDatabasePath", Required = true, HelpText = "Path to the existing aggregate SQLite database to read.")]
    public string InputDatabasePath { get; init; } = string.Empty;
}
