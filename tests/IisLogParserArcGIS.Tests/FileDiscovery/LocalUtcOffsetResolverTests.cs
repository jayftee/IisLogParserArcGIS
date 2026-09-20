using IisLogParserArcGIS.FileDiscovery;

namespace IisLogParserArcGIS.Tests.FileDiscovery;

public class LocalUtcOffsetResolverTests
{
    [Fact]
    public void Resolve_WithUtc_ReturnsZeroOffset()
    {
        var offset = LocalUtcOffsetResolver.Resolve("UTC", new DateOnly(2026, 5, 1));

        Assert.Equal(TimeSpan.Zero, offset);
    }

    [Fact]
    public void Resolve_WithTimeZoneBehindUtc_ReturnsNegativeOffset()
    {
        var offset = LocalUtcOffsetResolver.Resolve("America/New_York", new DateOnly(2026, 1, 15));

        Assert.True(offset < TimeSpan.Zero);
    }

    [Fact]
    public void Resolve_WithTimeZoneAheadOfUtc_ReturnsPositiveOffset()
    {
        var offset = LocalUtcOffsetResolver.Resolve("Asia/Tokyo", new DateOnly(2026, 5, 1));

        Assert.Equal(TimeSpan.FromHours(9), offset);
    }

    [Fact]
    public void Resolve_WithUnknownTimeZoneId_ThrowsTimeZoneNotFoundException()
    {
        Assert.Throws<TimeZoneNotFoundException>(() => LocalUtcOffsetResolver.Resolve("Not/A_RealZone", new DateOnly(2026, 5, 1)));
    }

    [Fact]
    public void Resolve_WithNullTimeZoneId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LocalUtcOffsetResolver.Resolve(null!, new DateOnly(2026, 5, 1)));
    }
}
