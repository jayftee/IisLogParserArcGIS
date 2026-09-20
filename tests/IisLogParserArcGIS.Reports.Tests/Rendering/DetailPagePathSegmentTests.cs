using IisLogParserArcGIS.Reports.Rendering;

namespace IisLogParserArcGIS.Reports.Tests.Rendering;

public class DetailPagePathSegmentTests
{
    [Theory]
    [InlineData("weird*service")]
    [InlineData("what?")]
    [InlineData("\"quoted\"")]
    [InlineData("a<b>c")]
    [InlineData("pipe|here")]
    [InlineData("colon:here")]
    [InlineData("back\\slash")]
    [InlineData("forward/slash")]
    public void Sanitize_WithIllegalCharacters_ContainsNoneOfThemAndChangesTheValue(string raw)
    {
        var sanitized = DetailPagePathSegment.Sanitize(raw);

        Assert.NotEqual(raw, sanitized);
        Assert.All("\"<>|:*?\\/", c => Assert.DoesNotContain(c, sanitized));
    }

    [Fact]
    public void Sanitize_ReplacesControlCharacters()
    {
        var raw = $"a{''}b{''}c";
        var sanitized = DetailPagePathSegment.Sanitize(raw);

        Assert.StartsWith("a_b_c-", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_WithOrdinaryValue_ReturnsItUnchanged()
    {
        Assert.Equal("mapserver", DetailPagePathSegment.Sanitize("mapserver"));
    }

    [Fact]
    public void Sanitize_WithEmptyValue_ReturnsUnderscore()
    {
        Assert.Equal("_", DetailPagePathSegment.Sanitize(string.Empty));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("...")]
    [InlineData(" ")]
    public void Sanitize_WithCurrentOrParentDirectorySegment_DoesNotResolveToDotOrDotDot(string traversalSegment)
    {
        var sanitized = DetailPagePathSegment.Sanitize(traversalSegment);

        Assert.NotEqual(".", sanitized);
        Assert.NotEqual("..", sanitized);
    }

    [Theory]
    [InlineData("con")]
    [InlineData("CON")]
    [InlineData("nul")]
    [InlineData("com1")]
    [InlineData("lpt9")]
    public void Sanitize_WithWindowsReservedDeviceName_DoesNotReturnItUnchanged(string reservedName)
    {
        Assert.NotEqual(reservedName, DetailPagePathSegment.Sanitize(reservedName), StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("services.")]
    [InlineData("services..")]
    public void Sanitize_WithTrailingDots_DoesNotSilentlyCollapseToTheUntrimmedName(string trailingDots)
    {
        var sanitized = DetailPagePathSegment.Sanitize(trailingDots);

        Assert.NotEqual("services", sanitized);
        Assert.StartsWith("services-", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_IsDeterministic_SameInputAlwaysProducesTheSameOutput()
    {
        Assert.Equal(DetailPagePathSegment.Sanitize("weird*service"), DetailPagePathSegment.Sanitize("weird*service"));
    }

    [Fact]
    public void Sanitize_DifferentRawValuesThatWouldOtherwiseCollide_ProduceDifferentResults()
    {
        var first = DetailPagePathSegment.Sanitize("item*1");
        var second = DetailPagePathSegment.Sanitize("item?1");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Sanitize_WithNullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DetailPagePathSegment.Sanitize(null!));
    }
}
