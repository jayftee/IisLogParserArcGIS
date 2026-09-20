using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class FieldMapsDeviceIdParserTests
{
    [Fact]
    public void TryParse_WithIosSwiftUserAgent_ReturnsCasefoldedDeviceId()
    {
        var result = FieldMapsDeviceIdParser.TryParse(
            "ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1+(227C3D43-EA74-4D05-AACA-1E47AC4BCDE9)",
            out var deviceId);

        Assert.True(result);
        Assert.Equal("227c3d43-ea74-4d05-aaca-1e47ac4bcde9", deviceId);
    }

    [Theory]
    [InlineData("ArcGISMaps-Swift/200.8+(iOS+26.2;+iPhone13,2)+arcgis-fieldmaps/25.3.1+(4819A98E-4393-4F68-9342-3CF29EFFF1C1)", "4819a98e-4393-4f68-9342-3cf29efff1c1")]
    [InlineData("ArcGISRuntime-iOS/100.15.6+(iOS+18.7;+iPhone14,5)+arcgis-fieldmaps/25.1.0+(D717DDC4-E46B-44D2-AAF6-3178334D9308)", "d717ddc4-e46b-44d2-aaf6-3178334d9308")]
    [InlineData("ArcGISRuntime-iOS/100.15+(iPadOS+18.7;+iPad13,4)+arcgis-fieldmaps/25.1.0+(D717DDC4-E46B-44D2-AAF6-3178334D9308)", "d717ddc4-e46b-44d2-aaf6-3178334d9308")]
    [InlineData("ArcGISMaps-Kotlin/200.8.1+(Android+16.0;+arm64-v8a;+SAMSUNG-SM-S938W)+arcgis-fieldmaps/26.1.1+(4f5a960e-b131-49dc-831f-da13fb6ffab9)", "4f5a960e-b131-49dc-831f-da13fb6ffab9")]
    [InlineData("ArcGISRuntime-Android/100.15+(Android+12.0;+arm64-v8a;+SAMSUNG-SM-A536W)++arcgis-fieldmaps/22.3.1+(4f5a960e-b131-49dc-831f-da13fb6ffab9)", "4f5a960e-b131-49dc-831f-da13fb6ffab9")]
    [InlineData("ArcGISMaps-Kotlin/300.1+(Android+12.0;+arm64-v8a;+MOTOROLA-MOTO-G-POWER-(2022))+arcgis-fieldmaps/26.2+(4f5a960e-b131-49dc-831f-da13fb6ffab9)", "4f5a960e-b131-49dc-831f-da13fb6ffab9")]
    public void TryParse_WithEachPlatformShape_ReturnsCasefoldedDeviceId(string userAgent, string expectedDeviceId)
    {
        var result = FieldMapsDeviceIdParser.TryParse(userAgent, out var deviceId);

        Assert.True(result);
        Assert.Equal(expectedDeviceId, deviceId);
    }

    [Fact]
    public void TryParse_WithUpperAndLowerCaseSpellingsOfTheSameGuid_ReturnsTheSameDeviceId()
    {
        var upperParsed = FieldMapsDeviceIdParser.TryParse("ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1+(ABCDEF01-2345-6789-ABCD-EF0123456789)", out var upper);
        var lowerParsed = FieldMapsDeviceIdParser.TryParse("ArcGISMaps-Kotlin/200.8.1+(Android+16.0;+arm64-v8a;+X)+arcgis-fieldmaps/26.1.1+(abcdef01-2345-6789-abcd-ef0123456789)", out var lower);

        Assert.True(upperParsed);
        Assert.True(lowerParsed);
        Assert.Equal(upper, lower);
    }

    [Theory]
    [InlineData("Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)")]
    [InlineData("ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1")]
    [InlineData("ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1+(227C3D43-EA74-4D05-AACA-1E47AC4BCDE9)+extra")]
    [InlineData("ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1+(227C3D43-EA74-4D05-AACA)")]
    [InlineData("Mozilla/5.0+(227C3D43-EA74-4D05-AACA-1E47AC4BCDE9)")]
    [InlineData("-")]
    [InlineData("")]
    public void TryParse_WithoutAFieldMapsTokenOrValidTrailingGuid_ReturnsFalse(string userAgent)
    {
        var result = FieldMapsDeviceIdParser.TryParse(userAgent, out var deviceId);

        Assert.False(result);
        Assert.Null(deviceId);
    }

    [Fact]
    public void TryParse_WithNullUserAgent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FieldMapsDeviceIdParser.TryParse(null!, out _));
    }

    [Fact]
    public void TryParse_WithTrailingGroupThatIsNotAGuid_ReturnsFalse()
    {
        var result = FieldMapsDeviceIdParser.TryParse("ArcGISMaps-Swift/200.8.1+(iOS+26.2;+iPhone15,5)+arcgis-fieldmaps/25.3.1+(not-a-guid)", out var deviceId);

        Assert.False(result);
        Assert.Null(deviceId);
    }
}
