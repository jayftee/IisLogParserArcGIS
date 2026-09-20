using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class LoginLineParserTests
{
    private const string WebAdaptorName = "portal";

    [Fact]
    public void TryParse_WithBareUserPath_ReturnsUsername()
    {
        var result = LoginLineParser.TryParse("/portal/sharing/rest/community/users/user00364@somewhere", WebAdaptorName, out var username);

        Assert.True(result);
        Assert.Equal("user00364@somewhere", username);
    }

    [Theory]
    [InlineData("/portal/sharing/rest/community/users/user00364@somewhere/userLicenseType")]
    [InlineData("/portal/sharing/rest/community/users/user00364@somewhere/some/deeper/resource")]
    public void TryParse_WithSubResourceAfterUsername_IgnoresTheSubResource(string path)
    {
        var result = LoginLineParser.TryParse(path, WebAdaptorName, out var username);

        Assert.True(result);
        Assert.Equal("user00364@somewhere", username);
    }

    [Theory]
    [InlineData("/Portal/sharing/rest/community/users/user00039@somewhere")]
    [InlineData("/PORTAL/SHARING/REST/COMMUNITY/USERS/user00039@somewhere")]
    public void TryParse_WithDifferentlyCasedStructuralSegments_StillMatches(string path)
    {
        var result = LoginLineParser.TryParse(path, WebAdaptorName, out var username);

        Assert.True(result);
        Assert.Equal("user00039@somewhere", username);
    }

    [Fact]
    public void TryParse_WithNonDefaultConfiguredWebAdaptorName_HonorsTheConfiguredName()
    {
        var matchesConfigured = LoginLineParser.TryParse("/gis/sharing/rest/community/users/jsmith", "gis", out var username);
        var matchesDefault = LoginLineParser.TryParse("/portal/sharing/rest/community/users/jsmith", "gis", out _);

        Assert.True(matchesConfigured);
        Assert.Equal("jsmith", username);
        Assert.False(matchesDefault);
    }

    [Theory]
    [InlineData("/portal/sharing/rest/community/users")]
    [InlineData("/portal/sharing/rest/community/groups/abc123")]
    [InlineData("/portal/sharing/rest/community/self")]
    [InlineData("/portal/sharing/rest/content/users/jsmith")]
    [InlineData("/arcgis/rest/services/Hosted/location_tracking/FeatureServer")]
    [InlineData("/portal/sharing/rest/portals/self")]
    [InlineData("/")]
    [InlineData("")]
    public void TryParse_WithAnyOtherPath_ReturnsFalse(string path)
    {
        var result = LoginLineParser.TryParse(path, WebAdaptorName, out var username);

        Assert.False(result);
        Assert.Null(username);
    }

    [Fact]
    public void TryParse_WithNullUriStem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoginLineParser.TryParse(null!, WebAdaptorName, out _));
    }

    [Fact]
    public void TryParse_WithNullWebAdaptorName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoginLineParser.TryParse("/portal/sharing/rest/community/users/jsmith", null!, out _));
    }

    [Fact]
    public void TryParse_WithMixedCaseUsername_ReturnsCasefoldedUsername()
    {
        var result = LoginLineParser.TryParse("/portal/sharing/rest/community/users/User00364@somewhere", WebAdaptorName, out var username);

        Assert.True(result);
        Assert.Equal("user00364@somewhere", username);
    }

    [Fact]
    public void TryParse_WithMixedCaseEmailStyleUsernameUnderMixedCasePortal_ReturnsCasefoldedUsername()
    {
        var result = LoginLineParser.TryParse("/Portal/sharing/rest/community/users/User00305@SOMEWHERE", WebAdaptorName, out var username);

        Assert.True(result);
        Assert.Equal("user00305@somewhere", username);
    }

    [Fact]
    public void TryParse_WithContentItemsPath_ReturnsFalse()
    {
        var result = LoginLineParser.TryParse("/portal/sharing/rest/content/items/abc123", WebAdaptorName, out var username);

        Assert.False(result);
        Assert.Null(username);
    }

    [Fact]
    public void TryParse_WithPercentEncodedEmailUsername_ReturnsDecodedUsername()
    {
        var result = LoginLineParser.TryParse("/portal/sharing/rest/community/users/user00353%40somewhere", WebAdaptorName, out var username);

        Assert.True(result);
        Assert.Equal("user00353@somewhere", username);
    }
}
