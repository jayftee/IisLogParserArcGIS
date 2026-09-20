using IisLogParserArcGIS.Domain.Aggregation;
using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class BySurvey123DeviceAggregatorTests
{
    private const string WebAdaptorName = "portal";
    private const string DeviceA = "0123456789abcdef0123456789abcdef";
    private const string DeviceB = "fedcba9876543210fedcba9876543210";
    private static readonly DateOnly _targetDate = new(2026, 5, 1);

    [Fact]
    public void Aggregate_WithPostLoginLineAndLaterUsage_ProducesOneAttributedRowSummingHitsAndTime()
    {
        var requests = new[]
        {
            Login("user00305@somewhere", DeviceA) with { TimeTakenMilliseconds = 1000 },
            Usage(DeviceA) with { TimeTakenMilliseconds = 2000 },
        };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(_targetDate, row.LocalDate);
        Assert.Equal(DeviceA, row.DeviceId);
        Assert.Equal("user00305@somewhere", row.Username);
        Assert.Equal(2, row.Hits);
        Assert.Equal(3.0, row.TimeTakenSecond);
    }

    [Fact]
    public void Aggregate_WithMixedCaseUsernameInTheLoginStem_StoresTheCasefoldedUsername()
    {
        var requests = new[] { Login("User00305@SOMEWHERE", DeviceA) };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Equal("user00305@somewhere", Assert.Single(rows).Username);
    }

    [Fact]
    public void Aggregate_WithRejectedLoginLine_DoesNotBindTheDeviceToTheUsername()
    {
        var requests = new[] { Login("someone", DeviceA) with { Status = 401 }, Usage(DeviceA) };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Null(row.Username);
        Assert.Equal(2, row.Hits);
    }

    [Fact]
    public void Aggregate_WithNoLoginForTheDevice_KeepsTheRowWithANullUsername()
    {
        var requests = new[] { Usage(DeviceA), Usage(DeviceA) };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Null(row.Username);
        Assert.Equal(2, row.Hits);
    }

    [Fact]
    public void Aggregate_AttributesOnlyTheDeviceThatLoggedIn()
    {
        var requests = new[] { Login("amy", DeviceA), Usage(DeviceB) };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Equal(2, rows.Count);
        Assert.Equal("amy", Assert.Single(rows, row => row.DeviceId == DeviceA).Username);
        Assert.Null(Assert.Single(rows, row => row.DeviceId == DeviceB).Username);
    }

    [Fact]
    public void Aggregate_WithTheSameDeviceIdInUpperAndLowerCase_MergesThemIntoOneRow()
    {
        var requests = new[]
        {
            Usage(DeviceA),
            Request("/mimas/rest/services/a/b/MapServer", UserAgent(DeviceA.ToUpperInvariant())),
        };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(DeviceA, row.DeviceId);
        Assert.Equal(2, row.Hits);
    }

    [Fact]
    public void Aggregate_WithSeveralUsernamesSuppliedOutOfOrder_KeepsTheEarliestLoginByTimestamp()
    {
        var requests = new[]
        {
            Login("later.user", DeviceA) with { UtcDateTime = new DateTime(2026, 5, 1, 15, 0, 0, DateTimeKind.Utc) },
            Login("earlier.user", DeviceA) with { UtcDateTime = new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc) },
        };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Equal("earlier.user", Assert.Single(rows).Username);
    }

    [Fact]
    public void Aggregate_WithLoginAppearingAfterTheUsage_StillAttributesTheWholeDay()
    {
        var requests = new[] { Usage(DeviceA), Usage(DeviceA), Login("amy", DeviceA) };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal("amy", row.Username);
        Assert.Equal(3, row.Hits);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsForOtherLocalDates_IncludingLoginsFromThoseDates()
    {
        var requests = new[]
        {
            Usage(DeviceA),
            Login("amy", DeviceA) with { LocalDate = _targetDate.AddDays(1) },
            Usage(DeviceA) with { LocalDate = _targetDate.AddDays(1) },
        };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        var row = Assert.Single(rows);
        Assert.Equal(1, row.Hits);
        Assert.Null(row.Username);
    }

    [Fact]
    public void Aggregate_IgnoresRequestsThatAreNotSurvey123Requests()
    {
        var requests = new[]
        {
            Request("/portal/sharing/rest/community/users/amy", "Mozilla/5.0+(Windows+NT+10.0)"),
            Request("/mimas/rest/services/a/b/MapServer", $"AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+{DeviceA})+QuickCapture/1.2.3+(Qt+5.15.6)+darwin/25.1.0"),
            Request("/mimas/rest/services/a/b/MapServer", "ArcGISRuntime-Qt/200.4.0+(iOS+18.1;+arm64;+Qt+5.15.6;+Survey123+3.21.86)"),
            Request("/mimas/rest/services/a/b/MapServer", "ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1+(227C3D43-EA74-4D05-AACA-1E47AC4BCDE9)"),
        };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, WebAdaptorName);

        Assert.Empty(rows);
    }

    [Fact]
    public void Aggregate_WithNonDefaultConfiguredWebAdaptorName_HonorsTheConfiguredName()
    {
        var requests = new[]
        {
            Request("/gis/sharing/rest/community/users/amy", UserAgent(DeviceA)),
            Usage(DeviceA),
        };

        var rows = BySurvey123DeviceAggregator.Aggregate(requests, _targetDate, "gis");

        Assert.Equal("amy", Assert.Single(rows).Username);
    }

    [Fact]
    public void Aggregate_WithNullRequests_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BySurvey123DeviceAggregator.Aggregate(null!, _targetDate, WebAdaptorName));
    }

    [Fact]
    public void Aggregate_WithNullPortalWebAdaptorName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BySurvey123DeviceAggregator.Aggregate([], _targetDate, null!));
    }

    private static string UserAgent(string deviceId) =>
        $"AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+{deviceId})+Survey123/3.24.21+(Qt+5.15.6;+arm64-little_endian-lp64;+iOS+26.1)+darwin/25.1.0";

    private static NormalizedLogRequest Usage(string deviceId) =>
        Request("/mimas/rest/services/wildfire/firemap/FeatureServer", UserAgent(deviceId));

    private static NormalizedLogRequest Login(string username, string deviceId) =>
        Request($"/portal/sharing/rest/community/users/{username}", UserAgent(deviceId));

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
