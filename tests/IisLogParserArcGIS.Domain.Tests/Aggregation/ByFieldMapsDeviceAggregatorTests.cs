using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ByFieldMapsDeviceAggregatorTests
{
    private const string WebAdaptorName = "portal";
    private const string DeviceA = "227c3d43-ea74-4d05-aaca-1e47ac4bcde9";
    private const string DeviceB = "4819a98e-4393-4f68-9342-3cf29efff1c1";
    private const string AndroidShape = "ArcGISMaps-Kotlin/200.8.1+(Android+16.0;+arm64-v8a;+X)+arcgis-fieldmaps/26.1.1+";
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithLoginLineAndLaterUsage_ProducesOneAttributedRowSummingHitsAndTime()
    {
        var requests = new[]
        {
            Login("user00364@somewhere", DeviceA) with { TimeTakenMilliseconds = 1000 },
            Usage(DeviceA) with { TimeTakenMilliseconds = 2000 },
        };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(_targetDate, row.LocalDate);
        Assert.Equal(DeviceA, row.DeviceId);
        Assert.Equal("user00364@somewhere", row.Username);
        Assert.Equal(2, row.Hits);
        Assert.Equal(3.0, row.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_WithRejectedLoginLine_DoesNotBindTheDeviceToTheUsername()
    {
        var requests = new[]
        {
            Login("user00385@somewhere", DeviceA) with { Status = 401 },
            Usage(DeviceA),
        };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Null(row.Username);
        Assert.Equal(2, row.Hits);
    }

    [Fact]
    public void Aggregate_WithNoLoginForTheDevice_KeepsTheRowWithANullUsername()
    {
        var requests = new[] { Usage(DeviceA), Usage(DeviceA) };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Null(row.Username);
        Assert.Equal(2, row.Hits);
    }

    [Fact]
    public void Aggregate_AttributesOnlyTheDeviceThatLoggedIn()
    {
        var requests = new[] { Login("user00364@somewhere", DeviceA), Usage(DeviceB) };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Equal(2, rows.Count);
        Assert.Equal("user00364@somewhere", Assert.Single(rows, row => row.DeviceId == DeviceA).Username);
        Assert.Null(Assert.Single(rows, row => row.DeviceId == DeviceB).Username);
    }

    [Fact]
    public void Aggregate_WithTheSameDeviceIdInUpperAndLowerCase_MergesThemIntoOneRow()
    {
        var requests = new[]
        {
            Usage(DeviceA),
            Request("/mimas/rest/services/a/b/MapServer", $"{AndroidShape}({DeviceA})"),
            Request("/mimas/rest/services/a/b/MapServer", $"{AndroidShape}({DeviceA.ToUpperInvariant()})"),
        };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(DeviceA, row.DeviceId);
        Assert.Equal(3, row.Hits);
    }

    [Fact]
    public void Aggregate_WithSeveralUsernamesForOneDevice_KeepsTheFirstLoginSeen()
    {
        var requests = new[]
        {
            Login("first.user", DeviceA),
            Login("second.user", DeviceA),
            Usage(DeviceA),
        };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Equal("first.user", Assert.Single(rows).Username);
    }

    [Fact]
    public void Aggregate_WithSeveralUsernamesSuppliedOutOfOrder_KeepsTheEarliestLoginByTimestamp()
    {
        var requests = new[]
        {
            Login("later.user", DeviceA) with { UtcDateTime = new DateTime(2026, 5, 1, 15, 0, 0, DateTimeKind.Utc) },
            Login("earlier.user", DeviceA) with { UtcDateTime = new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc) },
        };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Equal("earlier.user", Assert.Single(rows).Username);
    }

    [Fact]
    public void Aggregate_WithLoginAppearingAfterTheUsage_StillAttributesTheWholeDay()
    {
        var requests = new[] { Usage(DeviceA), Usage(DeviceA), Login("user00364@somewhere", DeviceA) };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal("user00364@somewhere", row.Username);
        Assert.Equal(3, row.Hits);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates_IncludingLoginsFromThoseDates()
    {
        var requests = new[]
        {
            Usage(DeviceA),
            Login("user00364@somewhere", DeviceA) with { LocalDate = _targetDate.AddDays(1) },
            Usage(DeviceA) with { LocalDate = _targetDate.AddDays(1) },
        };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
        Assert.Null(row.Username);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsThatAreNotFieldMapsRequests()
    {
        var requests = new[]
        {
            Request("/portal/sharing/rest/community/users/user00364@somewhere", "Mozilla/5.0+(Windows+NT+10.0)"),
            Request("/mimas/rest/services/a/b/MapServer", "ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1"),
        };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Empty(rows);
    }

    [Fact]
    public void Aggregate_WithNonDefaultConfiguredWebAdaptorName_HonorsTheConfiguredName()
    {
        var requests = new[]
        {
            Request("/gis/sharing/rest/community/users/user00364@somewhere", IosUserAgent(DeviceA)),
            Usage(DeviceA),
        };

        var rows = ByFieldMapsDeviceAggregator.Aggregate(requests, _targetDate, "gis");

        Assert.Equal("user00364@somewhere", Assert.Single(rows).Username);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByFieldMapsDeviceAggregator.Aggregate(null!, _targetDate, WebAdaptorName));
    }

    [Fact]
    public void Aggregate_WithNullPortalWebAdaptorName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ByFieldMapsDeviceAggregator.Aggregate([], _targetDate, null!));
    }

    private static string IosUserAgent(string deviceId) =>
        $"ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1+({deviceId.ToUpperInvariant()})";

    private static NormalizedLogRequest Usage(string deviceId) =>
        Request("/mimas/rest/services/wildfire/firemap/FeatureServer", IosUserAgent(deviceId));

    private static NormalizedLogRequest Login(string username, string deviceId) =>
        Request($"/portal/sharing/rest/community/users/{username}", IosUserAgent(deviceId));

    private static NormalizedLogRequest Request(string uriStem, string userAgent) =>
        new()
        {
            UtcDateTime = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            LocalDateTime = new DateTime(2026, 5, 1, 12, 0, 0),
            LocalDate = _targetDate,
            UriStem = uriStem,
            UserAgent = userAgent,
            Referer = "-",
            Status = 200,
            TimeTakenMilliseconds = 0,
            ForwardedFor = "-",
        };
}
