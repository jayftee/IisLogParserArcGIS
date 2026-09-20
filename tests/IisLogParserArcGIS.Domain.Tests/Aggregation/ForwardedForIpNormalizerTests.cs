using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Domain.Tests.Aggregation;

public class ForwardedForIpNormalizerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("-")]
    public void Normalize_WithAbsentField_ReturnsLoopbackPlaceholder(string input)
    {
        Assert.Equal("127.0.0.1", ForwardedForIpNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WithSingleIp_ReturnsIt()
    {
        var result = ForwardedForIpNormalizer.Normalize("203.0.113.1");

        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void Normalize_WithCommaSeparatedList_ReturnsFirstIp()
    {
        var result = ForwardedForIpNormalizer.Normalize("203.0.113.1, 70.41.3.18, 150.172.238.178");

        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void Normalize_WithColonSeparatedList_ReturnsFirstIp()
    {
        var result = ForwardedForIpNormalizer.Normalize("203.0.113.1:70.41.3.18");

        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void Normalize_WithPlusEncodedList_RemovesPlusAndReturnsFirstIp()
    {
        var result = ForwardedForIpNormalizer.Normalize("203.0.113.1,+70.41.3.18");

        Assert.Equal("203.0.113.1", result);
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("999.999.999.999")]
    public void Normalize_WithInvalidIp_ReturnsDash(string input)
    {
        Assert.Equal("-", ForwardedForIpNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WithNullForwardedFor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ForwardedForIpNormalizer.Normalize(null!));
    }
}
