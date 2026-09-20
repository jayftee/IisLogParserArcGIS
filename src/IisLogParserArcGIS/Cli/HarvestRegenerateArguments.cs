using CommandLine;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The <c>harvest-regenerate</c> verb's raw, unvalidated arguments: performs a Harvest Run for one Local Date,
/// then a Regeneration Run against the resulting aggregate database. The routine, single-date choice for a
/// day-to-day operator run.
/// </summary>
[Verb("harvest-regenerate", HelpText = "Harvest one Local Date's IIS log lines, then regenerate the entire Dashboard.")]
public sealed class HarvestRegenerateArguments : HarvestArgumentsBase
{
}
