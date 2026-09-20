using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class RootNormalizerTests
{
    [Theory]
    [InlineData("/ArcGIS/rest/services/Foo", "arcgis")]
    [InlineData("/foo", "foo")]
    [InlineData("//foo/bar", "foo")]
    public void Normalize_WithAPathSegment_ReturnsItLowercased(string input, string expected)
    {
        Assert.Equal(expected, RootNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("")]
    [InlineData("///")]
    public void Normalize_WithNoPathSegment_ReturnsFallback(string input)
    {
        Assert.Equal("-", RootNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WithSegmentAtThe16CharacterBoundary_IsNotTruncated()
    {
        var segment = new string('a', 16);

        var result = RootNormalizer.Normalize("/" + segment);

        Assert.Equal(16, result.Length);
        Assert.Equal(segment, result);
    }

    [Fact]
    public void Normalize_WithSegmentOverThe16CharacterBoundary_TruncatesTo16Characters()
    {
        var segment = new string('a', 17);

        var result = RootNormalizer.Normalize("/" + segment);

        Assert.Equal(16, result.Length);
        Assert.Equal(segment[..16], result);
    }

    [Fact]
    public void Normalize_WithNullUriStem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RootNormalizer.Normalize(null!));
    }
}
