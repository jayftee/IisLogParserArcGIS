using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class UriStemNormalizerTests
{
    [Fact]
    public void Normalize_WithUriStemAtThe1024CharacterBoundary_IsNotTruncated()
    {
        var uriStem = "/" + new string('a', 1023);

        var result = UriStemNormalizer.Normalize(uriStem);

        Assert.Equal(1024, result.Length);
        Assert.Equal(uriStem, result);
    }

    [Fact]
    public void Normalize_WithUriStemOverThe1024CharacterBoundary_TruncatesTo1024Characters()
    {
        var uriStem = "/" + new string('a', 1024);

        var result = UriStemNormalizer.Normalize(uriStem);

        Assert.Equal(1024, result.Length);
        Assert.Equal(uriStem[..1024], result);
    }

    [Fact]
    public void Normalize_WithNullUriStem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => UriStemNormalizer.Normalize(null!));
    }
}
