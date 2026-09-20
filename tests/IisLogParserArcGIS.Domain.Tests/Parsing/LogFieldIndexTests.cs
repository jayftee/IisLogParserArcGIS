using IisLogParserArcGIS.Domain.Parsing;

namespace IisLogParserArcGIS.Domain.Tests.Parsing;

public class LogFieldIndexTests
{
    [Fact]
    public void HasField_WithDeclaredField_ReturnsTrue()
    {
        var index = new LogFieldIndex(["date", "time", "cs-uri-stem"]);

        Assert.True(index.HasField("cs-uri-stem"));
    }

    [Fact]
    public void HasField_IsCaseInsensitive()
    {
        var index = new LogFieldIndex(["date", "time", "cs(User-Agent)"]);

        Assert.True(index.HasField("CS(USER-AGENT)"));
    }

    [Fact]
    public void HasField_WithUndeclaredField_ReturnsFalse()
    {
        var index = new LogFieldIndex(["date", "time"]);

        Assert.False(index.HasField("cs-uri-stem"));
    }

    [Fact]
    public void GetValue_ResolvesByDeclaredPosition()
    {
        var index = new LogFieldIndex(["cs-uri-stem", "date", "time"]);
        var rowFields = new[] { "/mimas/rest", "2026-05-01", "12:00:00" };

        Assert.Equal("/mimas/rest", index.GetValue(rowFields, "cs-uri-stem"));
        Assert.Equal("2026-05-01", index.GetValue(rowFields, "date"));
        Assert.Equal("12:00:00", index.GetValue(rowFields, "time"));
    }

    [Fact]
    public void FieldCount_ReflectsDeclaredFieldCount()
    {
        var index = new LogFieldIndex(["date", "time", "cs-uri-stem"]);

        Assert.Equal(3, index.FieldCount);
    }
}
