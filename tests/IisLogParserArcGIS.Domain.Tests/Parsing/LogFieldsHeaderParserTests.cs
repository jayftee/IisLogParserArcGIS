using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Parsing;

public class LogFieldsHeaderParserTests
{
    [Fact]
    public void TryParse_WithHeaderLine_ReturnsTrueAndFieldIndex()
    {
        var parsed = LogFieldsHeaderParser.TryParse("#Fields: date time cs-uri-stem", out var fieldIndex);

        Assert.True(parsed);
        Assert.NotNull(fieldIndex);
        Assert.Equal(3, fieldIndex.FieldCount);
        Assert.True(fieldIndex.HasField("cs-uri-stem"));
    }

    [Theory]
    [InlineData("#Software: Microsoft Internet Information Services 10.0")]
    [InlineData("#Version: 1.0")]
    [InlineData("2026-05-01 12:00:00 /mimas/rest")]
    public void TryParse_WithNonHeaderLine_ReturnsFalse(string line)
    {
        var parsed = LogFieldsHeaderParser.TryParse(line, out var fieldIndex);

        Assert.False(parsed);
        Assert.Null(fieldIndex);
    }
}
