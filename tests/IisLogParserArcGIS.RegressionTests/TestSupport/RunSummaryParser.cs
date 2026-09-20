using System.Globalization;
using System.Text.RegularExpressions;

namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// Parses the <see cref="RunSummary"/> line the shipped executable writes to stdout back out of its captured
/// output, matching the exact wording <c>RunSummaryReporter</c> logs.
/// </summary>
internal static partial class RunSummaryParser
{
    /// <summary>
    /// Parses the run summary line out of <paramref name="standardOutput"/>.
    /// </summary>
    /// <param name="standardOutput">The process's full captured stdout.</param>
    /// <returns>The parsed run summary.</returns>
    /// <exception cref="FormatException">No run summary line was found.</exception>
    public static RunSummary Parse(string standardOutput)
    {
        ArgumentNullException.ThrowIfNull(standardOutput);

        var match = SummaryPattern().Match(standardOutput);

        if (match.Success is false)
        {
            throw new FormatException($"Could not find a run summary line in the process's stdout:{Environment.NewLine}{standardOutput}");
        }

        return new RunSummary
        {
            Total = int.Parse(match.Groups["total"].Value, CultureInfo.InvariantCulture),
            Valid = int.Parse(match.Groups["valid"].Value, CultureInfo.InvariantCulture),
            Invalid = int.Parse(match.Groups["invalid"].Value, CultureInfo.InvariantCulture),
        };
    }

    [GeneratedRegex(@"Run summary: (?<total>\d+) line\(s\) read, (?<valid>\d+) valid, (?<invalid>\d+) invalid/skipped, elapsed [\d.,]+s\.")]
    private static partial Regex SummaryPattern();
}
