using IisLogParserArcGIS.Logging;
using Serilog.Events;

namespace IisLogParserArcGIS.Tests.Logging;

public class LogEventLevelResolverTests
{
    [Theory]
    [InlineData("Debug", LogEventLevel.Debug)]
    [InlineData("Warning", LogEventLevel.Warning)]
    [InlineData("warning", LogEventLevel.Warning)]
    [InlineData("ERROR", LogEventLevel.Error)]
    public void Resolve_WithRecognizedName_ReturnsMatchingLevel(string configuredLevel, LogEventLevel expected)
    {
        var resolved = LogEventLevelResolver.Resolve(configuredLevel, LogEventLevel.Fatal);

        Assert.Equal(expected, resolved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_WithBlankLevel_ReturnsFallback(string? configuredLevel)
    {
        var resolved = LogEventLevelResolver.Resolve(configuredLevel, LogEventLevel.Warning);

        Assert.Equal(LogEventLevel.Warning, resolved);
    }

    [Fact]
    public void Resolve_WithUnrecognizedName_ReturnsFallback()
    {
        var resolved = LogEventLevelResolver.Resolve("NotALevel", LogEventLevel.Warning);

        Assert.Equal(LogEventLevel.Warning, resolved);
    }

    [Fact]
    public void Resolve_WithUndefinedNumericValue_ReturnsFallback()
    {
        var resolved = LogEventLevelResolver.Resolve("42", LogEventLevel.Warning);

        Assert.Equal(LogEventLevel.Warning, resolved);
    }
}
