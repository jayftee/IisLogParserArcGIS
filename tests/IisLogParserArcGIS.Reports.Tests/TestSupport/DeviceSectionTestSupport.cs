using System.Text.Json;
using IisLogParserArcGIS.Reports.Configuration;
using IisLogParserArcGIS.Reports.Sections;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests.TestSupport;

/// <summary>
/// Constants and helpers shared by the three per-device section builder test classes (Fieldmaps, ticket 21;
/// Survey123, ticket 22), so each does not retype the same fixed run boundaries, settings, embedded-data
/// extraction and per-section theory data.
/// </summary>
public static class DeviceSectionTestSupport
{
    // The harvested fixture's corpus runs 2026-01-01 through 2026-09-10; pinning "today" to the corpus's last
    // date makes the All (year-to-date) range fall entirely within real harvested data.
    public static readonly DateOnly Today = new(2026, 9, 10);
    public static readonly DateOnly YearStart = new(2026, 1, 1);
    public static readonly DateOnly Last7DaysStart = Today.AddDays(-6);

    /// <summary>
    /// Gets xUnit theory data with one case per per-device section (<see cref="DeviceSection.All"/>), keyed by
    /// the section's slug; resolve it back with <see cref="SectionFor"/>.
    /// </summary>
    public static TheoryData<string> SectionSlugs
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var section in DeviceSection.All)
            {
                data.Add(section.SectionSlug);
            }

            return data;
        }
    }

    public static DeviceSection SectionFor(string sectionSlug)
    {
        return DeviceSection.All.Single(section => section.SectionSlug == sectionSlug);
    }

    /// <summary>
    /// Gets the per-device section that is not <paramref name="section"/>, for tests proving the two never show
    /// each other's devices.
    /// </summary>
    public static DeviceSection OtherSection(DeviceSection section)
    {
        return DeviceSection.All.Single(other => other.SectionSlug != section.SectionSlug);
    }

    public static FakeTimeProvider CreateTimeProvider()
    {
        return new FakeTimeProvider(new DateTimeOffset(Today, TimeOnly.MinValue, TimeSpan.Zero));
    }

    public static ReportsSettings CreateSettings(params string[] includedRoots)
    {
        return new ReportsSettings
        {
            LocalTimeZone = "UTC",
            IncludedRoots = includedRoots,
            PortalBaseUrl = "portal.example.com",
            OutputDirectory = "Dashboard",
        };
    }

    /// <summary>
    /// Extracts the row-major array embedded in a page's <c>arrayToDataTable(...)</c> call, header row first.
    /// </summary>
    public static JsonElement[] ExtractDataTableRows(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        const string Marker = "google.visualization.arrayToDataTable(";
        var start = html.IndexOf(Marker, StringComparison.Ordinal) + Marker.Length;
        var end = html.IndexOf(");", start, StringComparison.Ordinal);

        return JsonDocument.Parse(html[start..end]).RootElement.EnumerateArray().ToArray();
    }

    public static string ReadPage(string outputDirectory, string relativeHref)
    {
        ArgumentNullException.ThrowIfNull(relativeHref);

        return File.ReadAllText(Path.Combine(outputDirectory, relativeHref.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <summary>
    /// Reads a table cell's text: a plain string, or the raw <c>v</c> of a <c>{ v, f }</c> cell (an HTML-safe
    /// text cell, which sorts on <c>v</c> and displays <c>f</c>).
    /// </summary>
    public static string CellText(JsonElement cell)
    {
        var text = cell.ValueKind == JsonValueKind.Object ? cell.GetProperty("v") : cell;

        return text.GetString()!;
    }

#pragma warning disable CC0042 // Four independent row fields for a test-only fixture builder; bundling them would just repackage them.
    public static DeviceTestRow DeviceRow(string deviceId, string? username, DateOnly localDate, int hits) => new()
    {
        LocalDate = localDate,
        DeviceId = deviceId,
        Username = username,
        Hits = hits,
    };
#pragma warning restore CC0042
}
