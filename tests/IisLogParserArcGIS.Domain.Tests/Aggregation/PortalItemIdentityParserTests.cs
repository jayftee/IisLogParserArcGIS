using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class PortalItemIdentityParserTests
{
    private const string WebAdaptorName = "portal";

    [Fact]
    public void TryParse_WithDirectItemPath_ParsesItemIdAndIgnoresTrailingSubResource()
    {
        var result = PortalItemIdentityParser.TryParse("/portal/sharing/rest/content/items/abc123/data", WebAdaptorName, out var portalItemId);

        Assert.True(result);
        Assert.Equal("abc123", portalItemId);
    }

    [Fact]
    public void TryParse_WithUserScopedItemPath_ParsesItemIdAndDiscardsUsername()
    {
        var result = PortalItemIdentityParser.TryParse("/portal/sharing/rest/content/users/jsmith/items/abc123", WebAdaptorName, out var portalItemId);

        Assert.True(result);
        Assert.Equal("abc123", portalItemId);
    }

    [Fact]
    public void TryParse_WithUserAndFolderScopedItemPath_ParsesItemIdAndDiscardsUsernameAndFolder()
    {
        var result = PortalItemIdentityParser.TryParse("/portal/sharing/rest/content/users/jsmith/f00lder/items/abc123/update", WebAdaptorName, out var portalItemId);

        Assert.True(result);
        Assert.Equal("abc123", portalItemId);
    }

    [Fact]
    public void TryParse_WithNoTrailingSubResource_StillParsesItemId()
    {
        var result = PortalItemIdentityParser.TryParse("/portal/sharing/rest/content/items/abc123", WebAdaptorName, out var portalItemId);

        Assert.True(result);
        Assert.Equal("abc123", portalItemId);
    }

    [Theory]
    [InlineData("/portal/sharing/rest/content/users/jsmith/deleteItems")]
    [InlineData("/portal/sharing/rest/content/users/jsmith/shareItems")]
    [InlineData("/portal/sharing/rest/content/users/jsmith/f00lder/deleteItems")]
    public void TryParse_WithUserScopedBulkOperation_DoesNotMatch(string path)
    {
        var result = PortalItemIdentityParser.TryParse(path, WebAdaptorName, out var portalItemId);

        Assert.False(result);
        Assert.Null(portalItemId);
    }

    [Theory]
    [InlineData("/portal/sharing/rest/content/items")]
    [InlineData("/portal/sharing/rest/content/users/jsmith")]
    [InlineData("/portal/sharing/rest/content")]
    [InlineData("/portal/sharing/rest")]
    [InlineData("/portal/sharing/rest/portals/self")]
    [InlineData("/mimas/rest/services/wildfire/firemap/MapServer")]
    [InlineData("")]
    public void TryParse_WithNonItemShapedPath_DoesNotMatch(string path)
    {
        var result = PortalItemIdentityParser.TryParse(path, WebAdaptorName, out var portalItemId);

        Assert.False(result);
        Assert.Null(portalItemId);
    }

    [Fact]
    public void TryParse_WithNonDefaultConfiguredWebAdaptorName_HonorsTheConfiguredName()
    {
        var result = PortalItemIdentityParser.TryParse("/gis/sharing/rest/content/items/abc123", "gis", out var portalItemId);

        Assert.True(result);
        Assert.Equal("abc123", portalItemId);
    }

    [Fact]
    public void TryParse_WithDefaultConfiguredWebAdaptorName_DoesNotMatchANonDefaultPath()
    {
        var result = PortalItemIdentityParser.TryParse("/gis/sharing/rest/content/items/abc123", WebAdaptorName, out var portalItemId);

        Assert.False(result);
        Assert.Null(portalItemId);
    }

    [Fact]
    public void TryParse_WithInconsistentlyCasedPath_StillMatches()
    {
        var result = PortalItemIdentityParser.TryParse("/PORTAL/Sharing/REST/Content/Users/jsmith/ITEMS/abc123", WebAdaptorName, out var portalItemId);

        Assert.True(result);
        Assert.Equal("abc123", portalItemId);
    }

    [Fact]
    public void TryParse_WithItemIdLongerThan64Characters_TruncatesTo64Characters()
    {
        var longId = new string('a', 100);

        var result = PortalItemIdentityParser.TryParse($"/portal/sharing/rest/content/items/{longId}", WebAdaptorName, out var portalItemId);

        Assert.True(result);
        Assert.Equal(64, portalItemId!.Length);
        Assert.Equal(new string('a', 64), portalItemId);
    }

    [Fact]
    public void TryParse_WithNullUriStem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PortalItemIdentityParser.TryParse(null!, WebAdaptorName, out _));
    }

    [Fact]
    public void TryParse_WithNullPortalWebAdaptorName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PortalItemIdentityParser.TryParse("/portal/sharing/rest/content/items/abc123", null!, out _));
    }
}
