using CommandLine;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The <c>harvest</c> verb's raw, unvalidated arguments: parses one Local Date's IIS log lines and replaces
/// that date's Daily Batch in the aggregate database. Never touches the Dashboard.
/// </summary>
[Verb("harvest", HelpText = "Parse one Local Date's IIS log lines and replace that date's Daily Batch. Never touches the Dashboard.")]
public sealed class HarvestArguments : HarvestArgumentsBase
{
}
