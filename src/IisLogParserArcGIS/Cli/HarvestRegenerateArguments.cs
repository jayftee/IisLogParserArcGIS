using CommandLine;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The <c>harvest-regenerate</c> verb's raw, unvalidated arguments: performs a Harvest Run for one Local Date,
/// then a Regeneration Run against the resulting aggregate database for that date's year. The routine,
/// single-date choice for a day-to-day operator run.
/// </summary>
[Verb("harvest-regenerate", HelpText = "Harvest one Local Date's IIS log lines, then regenerate the entire Dashboard for that date's year.")]
public sealed class HarvestRegenerateArguments : HarvestArgumentsBase
{
    /// <summary>
    /// Gets the directory the Dashboard is generated into.
    /// </summary>
    [Value(3, MetaName = "outputDirectory", Required = true, HelpText = "Directory to generate the Dashboard into: new, empty, or holding a previous Dashboard (.iis-dashboard marker).")]
    public string OutputDirectory { get; init; } = string.Empty;
}
