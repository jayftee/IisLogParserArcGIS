using CommandLine;

namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The three raw, unvalidated positional arguments shared, verbatim, by every Harvest Run verb
/// (<see cref="HarvestArguments"/> and <see cref="HarvestRegenerateArguments"/>) - kept in exactly one place so
/// the two verbs cannot drift apart in their declared CLI surface.
/// </summary>
public abstract class HarvestArgumentsBase : IHarvestArguments
{
    /// <inheritdoc/>
    [Value(0, MetaName = "logSourceDirectory", Required = true, HelpText = "Directory containing the IIS W3C log files to process.")]
    public string LogSourceDirectory { get; init; } = string.Empty;

    /// <inheritdoc/>
    [Value(1, MetaName = "targetLocalDate", Required = true, HelpText = "Target local date to process, in yyyy-MM-dd format.")]
    public string TargetLocalDate { get; init; } = string.Empty;

    /// <inheritdoc/>
    [Value(2, MetaName = "outputDatabasePath", Required = true, HelpText = "Path to the output SQLite database file.")]
    public string OutputDatabasePath { get; init; } = string.Empty;
}
