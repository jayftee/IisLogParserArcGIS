using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Tests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests;

/// <summary>
/// Verifies every section's Complete View page carries its "Section » Complete View" title card above the
/// table, not just Summary's: drive <see cref="RegenerationRun.Run"/> and assert against each file it writes.
/// </summary>
[Collection(HarvestedAggregateDatabaseCollection.Name)]
public sealed class CompleteViewTitleCardTests
{
    private readonly HarvestedAggregateDatabaseFixture _fixture;

    public CompleteViewTitleCardTests(HarvestedAggregateDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("summary", "Summary")]
    [InlineData("portal", "Portal")]
    [InlineData("arcgis-server", "ArcGIS Server")]
    [InlineData("fieldmaps", "Fieldmaps")]
    [InlineData("survey123", "Survey123")]
    public void Run_CompleteView_ShowsTheSectionTitleCardAboveTheTable(string sectionDirectory, string sectionTitle)
    {
        using var outputDirectory = new TempDirectoryPath();
        using var connection = SqliteConnectionFactory.Open(_fixture.DatabasePath);
        var settings = new ReportsSettings
        {
            LocalTimeZone = "UTC",
            IncludedRoots = ["arcgis", "mimas", "titan"],
            PortalBaseUrl = "portal.example.com",
        };
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(new DateOnly(2026, 9, 10), TimeOnly.MinValue, TimeSpan.Zero));

        RegenerationRun.Run(connection, settings, timeProvider, outputDirectory.Path, 2026);

        var html = File.ReadAllText(Path.Combine(outputDirectory.Path, sectionDirectory, "complete-view.html"));

        Assert.Contains($"<div class=\"chart-title-card\"><h2>{sectionTitle} &#187; Complete View", html, StringComparison.Ordinal);
    }
}
