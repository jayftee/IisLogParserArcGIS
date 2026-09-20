using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class DeviceAttributionCountsTests
{
    private static readonly DateOnly _date = new(2026, 5, 1);

    [Fact]
    public void Create_SplitsDevicesIntoSameDayBackfilledAndStillUnattributed()
    {
        ByFieldMapsDeviceAggregateRow[] aggregatedRows = [Row("a", "wf.one"), Row("b", null), Row("c", null), Row("d", null)];
        ByFieldMapsDeviceAggregateRow[] persistedRows = [Row("a", "wf.one"), Row("b", "wf.two"), Row("c", null), Row("d", null)];

        var counts = DeviceAttributionCounts.Create(aggregatedRows, persistedRows);

        Assert.Equal(4, counts.DevicesSeen);
        Assert.Equal(1, counts.AttributedSameDay);
        Assert.Equal(1, counts.AttributedByBackfill);
        Assert.Equal(2, counts.Unattributed);
    }

    [Fact]
    public void Create_WithSurvey123Rows_CountsThemTheSameWay()
    {
        BySurvey123DeviceAggregateRow[] aggregatedRows = [Survey123Row("a", "amy"), Survey123Row("b", null), Survey123Row("c", null)];
        BySurvey123DeviceAggregateRow[] persistedRows = [Survey123Row("a", "amy"), Survey123Row("b", "bob"), Survey123Row("c", null)];

        var counts = DeviceAttributionCounts.Create(aggregatedRows, persistedRows);

        Assert.Equal(new DeviceAttributionCounts(AttributedSameDay: 1, AttributedByBackfill: 1, Unattributed: 1), counts);
    }

    [Fact]
    public void Create_WithNoDevices_ReturnsAllZeros()
    {
        var counts = DeviceAttributionCounts.Create([], []);

        Assert.Equal(new DeviceAttributionCounts(0, 0, 0), counts);
    }

    [Fact]
    public void Create_WithNullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DeviceAttributionCounts.Create(null!, []));
        Assert.Throws<ArgumentNullException>(() => DeviceAttributionCounts.Create([], null!));
    }

    private static BySurvey123DeviceAggregateRow Survey123Row(string deviceId, string? username) =>
        new()
        {
            LocalDate = _date,
            DeviceId = deviceId,
            Username = username,
            TimeTakenSecond = 1.0,
            Hits = 1,
        };

    private static ByFieldMapsDeviceAggregateRow Row(string deviceId, string? username) =>
        new()
        {
            LocalDate = _date,
            DeviceId = deviceId,
            Username = username,
            TimeTakenSecond = 1.0,
            Hits = 1,
        };
}
