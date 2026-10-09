using System.Text;
using IisLogParserArcGIS.Reports.Rendering;
using IisLogParserArcGIS.Reports.Tests.TestSupport;

namespace IisLogParserArcGIS.Reports.Tests.Rendering;

/// <summary>
/// Verifies <see cref="TsvWriter"/> directly: a pure, file-writing helper with no aggregate-database dependency,
/// so these tests build small in-memory tables by hand rather than driving <see cref="RegenerationRun.Run"/>.
/// </summary>
public sealed class TsvWriterTests
{
    [Fact]
    public void Write_PlainCells_WritesTabSeparatedRowsWithLfLineEndings()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows =
        [
            ["Root", "Hits", "Average Time Taken (s)"],
            ["titan", 42, 0.28],
        ];

        TsvWriter.Write(outputDirectory.Path, "summary/complete-view.tsv", rows);

        var written = File.ReadAllText(Path.Combine(outputDirectory.Path, "summary", "complete-view.tsv"));

        Assert.Equal("Root\tHits\tAverage Time Taken (s)\ntitan\t42\t0.28\n", written);
    }

    [Fact]
    public void Write_UsesUtf8WithoutABom()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows = [["Root"], ["titan"]];

        TsvWriter.Write(outputDirectory.Path, "complete-view.tsv", rows);

        var bytes = File.ReadAllBytes(Path.Combine(outputDirectory.Path, "complete-view.tsv"));

        Assert.NotEqual(Encoding.UTF8.GetPreamble(), bytes.Take(3).ToArray());
    }

    [Fact]
    public void Write_ADictionaryShapedVfCell_WritesOnlyThePlainValue()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows =
        [
            ["Service Type"],
            [VfCell("<svg/onload=alert(1)>Server", "&lt;svg/onload=alert(1)&gt;Server")],
        ];

        TsvWriter.Write(outputDirectory.Path, "complete-view.tsv", rows);

        var written = File.ReadAllText(Path.Combine(outputDirectory.Path, "complete-view.tsv"));

        Assert.Equal("Service Type\n<svg/onload=alert(1)>Server\n", written);
    }

    [Fact]
    public void Write_AnAnonymousVfCell_WritesOnlyThePlainValue()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows =
        [
            ["Portal Item"],
            [new { v = "abc123", f = "<a href=\"https://example.com\">abc123</a>" }],
        ];

        TsvWriter.Write(outputDirectory.Path, "complete-view.tsv", rows);

        var written = File.ReadAllText(Path.Combine(outputDirectory.Path, "complete-view.tsv"));

        Assert.Equal("Portal Item\nabc123\n", written);
    }

    [Fact]
    public void Write_WithTrailingColumnsToDrop_OmitsThemFromEveryRow()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows =
        [
            ["Portal Item", "Hits", "Detail Page"],
            ["abc123", 7, "<a href=\"portal/detail/abc123.html\">Details</a>"],
        ];

        TsvWriter.Write(outputDirectory.Path, "complete-view.tsv", rows, trailingColumnsToDrop: 1);

        var written = File.ReadAllText(Path.Combine(outputDirectory.Path, "complete-view.tsv"));

        Assert.Equal("Portal Item\tHits\nabc123\t7\n", written);
    }

    [Fact]
    public void Write_ACellWithEmbeddedTabCrAndLf_ReplacesEachWithASingleSpace()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows =
        [
            ["Folder"],
            [VfCell("svc\tname\r\nhere", "svc\tname\r\nhere")],
        ];

        TsvWriter.Write(outputDirectory.Path, "complete-view.tsv", rows);

        var written = File.ReadAllText(Path.Combine(outputDirectory.Path, "complete-view.tsv"));

        Assert.Equal("Folder\nsvc name  here\n", written);
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+1+1")]
    [InlineData("-1+1")]
    [InlineData("@SUM(A1:A2)")]
    public void Write_AStringCellStartingWithAFormulaTriggerCharacter_IsPrefixedWithAQuote(string attackerValue)
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows = [["Device ID"], [VfCell(attackerValue, attackerValue)]];

        TsvWriter.Write(outputDirectory.Path, "complete-view.tsv", rows);

        var written = File.ReadAllText(Path.Combine(outputDirectory.Path, "complete-view.tsv"));

        Assert.Equal($"Device ID\n'{attackerValue}\n", written);
    }

    [Fact]
    public void Write_ANegativeNumericCell_IsNotPrefixedWithAQuote()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows = [["Average Time Taken (s)"], [-1.5]];

        TsvWriter.Write(outputDirectory.Path, "complete-view.tsv", rows);

        var written = File.ReadAllText(Path.Combine(outputDirectory.Path, "complete-view.tsv"));

        Assert.Equal("Average Time Taken (s)\n-1.5\n", written);
    }

    [Fact]
    public void Write_ANullCell_WritesAnEmptyColumn()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows = [["Username", "Hits"], [null, 3]];

        TsvWriter.Write(outputDirectory.Path, "complete-view.tsv", rows);

        var written = File.ReadAllText(Path.Combine(outputDirectory.Path, "complete-view.tsv"));

        Assert.Equal("Username\tHits\n\t3\n", written);
    }

    [Fact]
    public void WriteSiblingAndRenderDownloadLink_WithOnlyAHeaderRow_WritesNoFileAndReturnsEmptyString()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows = [["Root", "Hits"]];

        var linkHtml = TsvWriter.WriteSiblingAndRenderDownloadLink(outputDirectory.Path, "summary/complete-view.html", rows);

        Assert.Equal(string.Empty, linkHtml);
        Assert.False(File.Exists(Path.Combine(outputDirectory.Path, "summary", "complete-view.tsv")));
    }

    [Fact]
    public void WriteSiblingAndRenderDownloadLink_WithData_WritesTheSiblingAndLinksToItsBareFileName()
    {
        using var outputDirectory = new TempDirectoryPath();
        List<IReadOnlyList<object?>> rows = [["Root", "Hits"], ["titan", 1]];

        var linkHtml = TsvWriter.WriteSiblingAndRenderDownloadLink(outputDirectory.Path, "summary/complete-view.html", rows);

        Assert.True(File.Exists(Path.Combine(outputDirectory.Path, "summary", "complete-view.tsv")));
        Assert.Equal("""<p class="tsv-download-link"><a href="complete-view.tsv">Download as TSV</a></p>""", linkHtml);
    }

    // Mirrors HtmlSafeTableCell.Create's own { v, f } dictionary shape without depending on that internal type.
    private static Dictionary<string, string> VfCell(string value, string formatted) =>
        new() { ["v"] = value, ["f"] = formatted };
}
