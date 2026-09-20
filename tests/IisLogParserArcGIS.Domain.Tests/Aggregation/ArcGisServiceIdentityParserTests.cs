using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ArcGisServiceIdentityParserTests
{
    [Fact]
    public void TryParse_WithFolderedService_ParsesIdentityAndIgnoresOperationSegment()
    {
        var result = ArcGisServiceIdentityParser.TryParse("/mimas/rest/services/wildfire/firemap/MapServer/export", out var identity);

        Assert.True(result);
        Assert.Equal("mimas", identity!.Site);
        Assert.Equal("wildfire", identity.Folder);
        Assert.Equal("firemap", identity.ServiceName);
        Assert.Equal("mapserver", identity.ServiceType);
    }

    [Fact]
    public void TryParse_WithFolderlessService_ParsesIdentityWithNullFolder()
    {
        var result = ArcGisServiceIdentityParser.TryParse("/mimas/rest/services/watermap/FeatureServer/export", out var identity);

        Assert.True(result);
        Assert.Equal("mimas", identity!.Site);
        Assert.Null(identity.Folder);
        Assert.Equal("watermap", identity.ServiceName);
        Assert.Equal("featureserver", identity.ServiceType);
    }

    [Fact]
    public void TryParse_WithNoOperationSegment_StillParsesIdentity()
    {
        var result = ArcGisServiceIdentityParser.TryParse("/mimas/rest/services/wildfire/firemap/MapServer", out var identity);

        Assert.True(result);
        Assert.Equal("wildfire", identity!.Folder);
        Assert.Equal("firemap", identity.ServiceName);
        Assert.Equal("mapserver", identity.ServiceType);
    }

    [Fact]
    public void TryParse_WithFolderedServiceWhoseNameLooksLikeAServiceType_StillPrefersTheFolderedInterpretation()
    {
        var result = ArcGisServiceIdentityParser.TryParse("/mimas/rest/services/reports/GeocodeServer/MapServer", out var identity);

        Assert.True(result);
        Assert.Equal("reports", identity!.Folder);
        Assert.Equal("geocodeserver", identity.ServiceName);
        Assert.Equal("mapserver", identity.ServiceType);
    }

    [Fact]
    public void TryParse_WithInconsistentlyCasedPath_NormalizesFolderNameAndTypeToLowercase()
    {
        var result = ArcGisServiceIdentityParser.TryParse("/titan/rest/services/Environment/Alberta_Watersheds/MapServer", out var identity);

        Assert.True(result);
        Assert.Equal("environment", identity!.Folder);
        Assert.Equal("alberta_watersheds", identity.ServiceName);
        Assert.Equal("mapserver", identity.ServiceType);
    }

    [Theory]
    [InlineData("/mimas/admin/machines")]
    [InlineData("/mimas/admin/services/wildfire/firemap/MapServer")]
    public void TryParse_WithAdminTraffic_DoesNotParse(string path)
    {
        var result = ArcGisServiceIdentityParser.TryParse(path, out var identity);

        Assert.False(result);
        Assert.Null(identity);
    }

    [Theory]
    [InlineData("/portal/sharing/rest/portals/self")]
    [InlineData("/portal/rest/services/wildfire/firemap/MapServer")]
    public void TryParse_WithPortalTraffic_DoesNotParse(string path)
    {
        var result = ArcGisServiceIdentityParser.TryParse(path, out var identity);

        Assert.False(result);
        Assert.Null(identity);
    }

    [Theory]
    [InlineData("/test/service/?foo=bar")]
    [InlineData("/portal/sharing/rest")]
    [InlineData("/mimas/rest/servicesnot/wildfire/firemap/MapServer")]
    [InlineData("/mimas/rest/services/wildfire")]
    [InlineData("/mimas/rest/services")]
    [InlineData("/mimas")]
    [InlineData("")]
    public void TryParse_WithNonServiceShapedPath_DoesNotParse(string path)
    {
        var result = ArcGisServiceIdentityParser.TryParse(path, out var identity);

        Assert.False(result);
        Assert.Null(identity);
    }

    [Fact]
    public void TryParse_WithNullUriStem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ArcGisServiceIdentityParser.TryParse(null!, out _));
    }
}
