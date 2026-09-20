using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class Survey123DeviceIdParserTests
{
    private const string DeviceId = "0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData("AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+0123456789abcdef0123456789abcdef)+Survey123/3.24.21+(Qt+5.15.6;+arm64-little_endian-lp64;+iOS+26.1)+darwin/25.1.0")]
    [InlineData("AppFramework/5.5.204+(Windows+10;+en_CA;+x86_64;+0123456789abcdef0123456789abcdef)+Survey123/3.24.21+(Qt+5.15.6;+x86_64;+Windows+10)+winnt/10.0.19045")]
    [InlineData("AppFramework/5.5.204+(Android+16;+en_CA;+arm64;+0123456789abcdef0123456789abcdef)+Survey123/3.24.21+(Qt+5.15.6;+arm64;+Android++(16))+linux/6.1.134-android14-11")]
    [InlineData("AppFramework/5.5.204+(Windows+10;+en_US;+x86_64;+0123456789abcdef0123456789abcdef)+Survey123+Connect/3.24.21+(Qt+5.15.6;+x86_64;+Windows+10)+winnt/10.0.19045")]
    public void TryParse_WithEachSurvey123Shape_ReturnsTheDeviceId(string userAgent)
    {
        var result = Survey123DeviceIdParser.TryParse(userAgent, out var deviceId);

        Assert.True(result);
        Assert.Equal(DeviceId, deviceId);
    }

    [Fact]
    public void TryParse_WithAnUppercaseId_ReturnsItCasefoldedWithoutHyphens()
    {
        var result = Survey123DeviceIdParser.TryParse(
            "AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+0123456789ABCDEF0123456789ABCDEF)+Survey123/3.24.21+(Qt+5.15.6)+darwin/25.1.0",
            out var deviceId);

        Assert.True(result);
        Assert.Equal(DeviceId, deviceId);
    }

    [Fact]
    public void TryParse_WhenTheIdIsNotTheLastItemOfTheGroup_TakesTheLastItem()
    {
        var result = Survey123DeviceIdParser.TryParse(
            "AppFramework/5.5.204+(iOS+26.1;+0123456789abcdef0123456789abcdef;+arm64;+fedcba9876543210fedcba9876543210)+Survey123/3.24.21",
            out var deviceId);

        Assert.True(result);
        Assert.Equal("fedcba9876543210fedcba9876543210", deviceId);
    }

    [Theory]
    [InlineData("AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+0123456789abcdef0123456789abcdef)+QuickCapture/1.2.3+(Qt+5.15.6)+darwin/25.1.0")]
    [InlineData("ArcGISRuntime-Qt/200.4.0+(iOS+18.1;+arm64;+Qt+5.15.6;+Survey123+3.21.86)")]
    [InlineData("ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1+(227C3D43-EA74-4D05-AACA-1E47AC4BCDE9)")]
    [InlineData("AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+notahexstringnotahexstringnotahe)+Survey123/3.24.21")]
    [InlineData("AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+0123456789abcdef0123456789abcde)+Survey123/3.24.21")]
    [InlineData("AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+0123456789abcdef0123456789abcdef0)+Survey123/3.24.21")]
    [InlineData("AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+01234567-89ab-cdef-0123-456789abcdef)+Survey123/3.24.21")]
    [InlineData("AppFramework/5.5.204+Survey123/3.24.21+darwin/25.1.0")]
    [InlineData("AppFramework/5.5.204+(iOS+26.1;+en_CA;+arm64;+0123456789abcdef0123456789abcdef+Survey123/3.24.21")]
    [InlineData("Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)")]
    [InlineData("-")]
    [InlineData("")]
    public void TryParse_WithoutBothTokensOrAValid32HexId_ReturnsFalse(string userAgent)
    {
        var result = Survey123DeviceIdParser.TryParse(userAgent, out var deviceId);

        Assert.False(result);
        Assert.Null(deviceId);
    }

    [Fact]
    public void TryParse_WithNullUserAgent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Survey123DeviceIdParser.TryParse(null!, out _));
    }
}
