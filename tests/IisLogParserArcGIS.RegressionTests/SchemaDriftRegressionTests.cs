using System.Globalization;
using IisLogParserArcGIS.RegressionTests.TestSupport;

namespace IisLogParserArcGIS.RegressionTests;

/// <summary>
/// Proves the shipped executable resolves every field by name from each physical file's own governing
/// <c>#Fields</c> header, against the corpus's confirmed 16-vs-18-field schema drift, rather than assuming a
/// fixed field count or position.
/// </summary>
public sealed class SchemaDriftRegressionTests
{
    // 2026-01-01: both of the day's files declare the 16-field header.
    // 2026-03-06: the day's two files declare *different* field counts (18 fields vs. 16) for the same UTC date -
    // the parser must resolve each file's fields from its own header, not a run-wide assumption.
    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2026-03-06")]
    public async Task ParsesEveryLineOnADayWithNoFieldCountMismatches(string targetLocalDateText)
    {
        var targetLocalDate = DateOnly.ParseExact(targetLocalDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var database = new TempOutputDatabase();

        var summary = await RegressionAssertions.RunAndAssertCoreInvariantsAsync(targetLocalDate, database.Path);

        Assert.Equal(0, summary.Invalid);
    }
}
