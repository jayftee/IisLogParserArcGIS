namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The three raw, unvalidated positional arguments shared by every verb that performs a Harvest Run
/// (<see cref="HarvestArguments"/> and <see cref="HarvestRegenerateArguments"/>).
/// </summary>
public interface IHarvestArguments
{
    /// <summary>
    /// Gets the directory containing the IIS W3C log files to process.
    /// </summary>
    string LogSourceDirectory { get; }

    /// <summary>
    /// Gets the target local date to process, as raw text in <c>yyyy-MM-dd</c> format.
    /// </summary>
    string TargetLocalDate { get; }

    /// <summary>
    /// Gets the path to the output SQLite database file.
    /// </summary>
    string OutputDatabasePath { get; }
}
