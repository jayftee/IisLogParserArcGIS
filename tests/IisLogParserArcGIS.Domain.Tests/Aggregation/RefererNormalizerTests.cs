using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class RefererNormalizerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("-")]
    public void Normalize_WithEmptyOrDash_ReturnsDash(string input)
    {
        Assert.Equal("-", RefererNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WithAllPlusCharacters_DecodesToWhitespaceThenReturnsDash()
    {
        var result = RefererNormalizer.Normalize("+++");

        Assert.Equal("-", result);
    }

    [Fact]
    public void Normalize_WithPlusEncodedReferer_DecodesPlusToSpace()
    {
        var result = RefererNormalizer.Normalize("https://example.com/some+page");

        Assert.Equal("https://example.com/some page", result);
    }

    [Fact]
    public void Normalize_WithMixedCaseReferer_Lowercases()
    {
        var result = RefererNormalizer.Normalize("https://Example.COM/Page");

        Assert.Equal("https://example.com/page", result);
    }

    [Fact]
    public void Normalize_WithSurroundingWhitespace_Trims()
    {
        var result = RefererNormalizer.Normalize("  https://example.com/page  ");

        Assert.Equal("https://example.com/page", result);
    }

    [Fact]
    public void Normalize_WithOpaqueNonUrlReferer_AcceptsAsIs()
    {
        var result = RefererNormalizer.Normalize("com.esri.arcgis.rest.SomeInternalHandler");

        Assert.Equal("com.esri.arcgis.rest.someinternalhandler", result);
    }

    [Fact]
    public void Normalize_WithRefererAtThe4096CharacterBoundary_IsNotTruncated()
    {
        var referer = new string('a', 4096);

        var result = RefererNormalizer.Normalize(referer);

        Assert.Equal(4096, result.Length);
        Assert.Equal(referer, result);
    }

    [Fact]
    public void Normalize_WithRefererOverThe4096CharacterBoundary_TruncatesTo4096Characters()
    {
        var referer = new string('a', 4097);

        var result = RefererNormalizer.Normalize(referer);

        Assert.Equal(4096, result.Length);
        Assert.Equal(referer[..4096], result);
    }

    [Fact]
    public void Normalize_WithNullReferer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RefererNormalizer.Normalize(null!));
    }
}
