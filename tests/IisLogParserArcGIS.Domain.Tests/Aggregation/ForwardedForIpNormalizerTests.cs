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

    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("2001:db8:85a3::8a2e:370:7334")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("::ffff:203.0.113.1")]
    public void Normalize_WithIpv6Address_ReturnsItWhole(string input)
    {
        Assert.Equal(input, ForwardedForIpNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WithDifferentIpv6AddressesSharingTheFirstGroup_KeepsThemApart()
    {
        var first = ForwardedForIpNormalizer.Normalize("2001:db8::1");
        var second = ForwardedForIpNormalizer.Normalize("2001:db8::2");

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("[2001:db8::1]", "2001:db8::1")]
    [InlineData("[2001:db8::1]:443", "2001:db8::1")]
    [InlineData("[::1]:8080", "::1")]
    public void Normalize_WithBracketedIpv6Address_ReturnsTheAddressWithoutBracketsAndPort(string input, string expected)
    {
        Assert.Equal(expected, ForwardedForIpNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WithCommaSeparatedListStartingWithIpv6_ReturnsTheIpv6Address()
    {
        var result = ForwardedForIpNormalizer.Normalize("2001:db8::1, 203.0.113.1");

        Assert.Equal("2001:db8::1", result);
    }

    [Fact]
    public void Normalize_WithIpv4AndPort_ReturnsTheIpv4Address()
    {
        var result = ForwardedForIpNormalizer.Normalize("203.0.113.1:8080");

        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void Normalize_WithTheRealWorldShapeOfIpv4PortFollowedByAProxyChain_ReturnsTheClientIpv4()
    {
        var result = ForwardedForIpNormalizer.Normalize("142.59.220.47:50169,10.87.7.4,+20.69.115.185");

        Assert.Equal("142.59.220.47", result);
    }

    [Theory]
    [InlineData("2001:db8::zz")]
    [InlineData("2001:zz")]
    [InlineData("[2001:db8::zz]:443")]
    [InlineData("[not-an-ip]")]
    public void Normalize_WithAnInvalidIpv6LookalikeOrBadBracketedValue_ReturnsDashInsteadOfItsFirstGroup(string input)
    {
        Assert.Equal("-", ForwardedForIpNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WithAnOverlongIpv6Address_TruncatesToFortyEightCharacters()
    {
        var result = ForwardedForIpNormalizer.Normalize("fe80::1%" + new string('a', 60));

        Assert.Equal(48, result.Length);
    }

    [Fact]
    public void Normalize_WithNullForwardedFor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ForwardedForIpNormalizer.Normalize(null!));
    }
}
