namespace IisLogParserArcGIS.Cli;

/// <summary>
/// The <c>harvest-regenerate</c> verb's command-line arguments after validation and normalization: the Harvest
/// Run's own arguments, plus the directory the Dashboard is generated into.
/// </summary>
/// <param name="Harvest">The validated arguments of the Harvest Run half.</param>
/// <param name="OutputDirectory">The full path of the directory the Dashboard is generated into.</param>
public sealed record ParsedHarvestRegenerateArguments(ParsedArguments Harvest, string OutputDirectory)
{
    /// <summary>
    /// Gets the calendar year the Dashboard reports on: the year of the harvested Local Date.
    /// </summary>
    public int Year => Harvest.TargetLocalDate.Year;
}
