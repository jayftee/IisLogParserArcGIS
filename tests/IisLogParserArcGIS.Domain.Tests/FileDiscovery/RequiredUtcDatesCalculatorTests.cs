using IisLogParserArcGIS.Domain.FileDiscovery;

namespace IisLogParserArcGIS.Domain.Tests.FileDiscovery;

public class RequiredUtcDatesCalculatorTests
{
    [Fact]
    public void Calculate_WithZeroOffset_ReturnsOnlyTargetDate()
    {
        var targetLocalDate = new DateOnly(2026, 5, 1);

        var result = RequiredUtcDatesCalculator.Calculate(targetLocalDate, TimeSpan.Zero);

        Assert.Equal(new[] { targetLocalDate }, result);
    }

    [Fact]
    public void Calculate_WithNegativeOffset_ReturnsTargetDateAndDayAfter()
    {
        var targetLocalDate = new DateOnly(2026, 5, 1);

        var result = RequiredUtcDatesCalculator.Calculate(targetLocalDate, TimeSpan.FromHours(-5));

        Assert.Equal(new[] { targetLocalDate, targetLocalDate.AddDays(1) }, result);
    }

    [Fact]
    public void Calculate_WithPositiveOffset_ReturnsDayBeforeAndTargetDate()
    {
        var targetLocalDate = new DateOnly(2026, 5, 1);

        var result = RequiredUtcDatesCalculator.Calculate(targetLocalDate, TimeSpan.FromHours(9));

        Assert.Equal(new[] { targetLocalDate.AddDays(-1), targetLocalDate }, result);
    }

    [Fact]
    public void Calculate_WithNegativeOffset_AcrossYearBoundary_RollsOverCorrectly()
    {
        var targetLocalDate = new DateOnly(2026, 12, 31);

        var result = RequiredUtcDatesCalculator.Calculate(targetLocalDate, TimeSpan.FromHours(-5));

        Assert.Equal(new[] { targetLocalDate, new DateOnly(2027, 1, 1) }, result);
    }

    [Fact]
    public void Calculate_WithPositiveOffset_AcrossYearBoundary_RollsOverCorrectly()
    {
        var targetLocalDate = new DateOnly(2026, 1, 1);

        var result = RequiredUtcDatesCalculator.Calculate(targetLocalDate, TimeSpan.FromHours(9));

        Assert.Equal(new[] { new DateOnly(2025, 12, 31), targetLocalDate }, result);
    }
}
