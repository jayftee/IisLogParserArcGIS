using IisLogParserArcGIS.Reports;
using Microsoft.Extensions.Time.Testing;

namespace IisLogParserArcGIS.Reports.Tests;

public class RegenerationRunBoundariesTests
{
    [Fact]
    public void Resolve_WithUtcTimeZone_UsesUtcCalendarDate()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, "UTC", 2026);

        Assert.Equal(new DateOnly(2026, 6, 15), boundaries.Today);
        Assert.Equal(new DateOnly(2026, 1, 1), boundaries.YearStart);
    }

    [Fact]
    public void Resolve_WhenLocalOffsetCrossesIntoTheNextCalendarDate_UsesTheLocalDate()
    {
        var lateEveningUtc = new DateTimeOffset(2026, 6, 15, 23, 30, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(lateEveningUtc);

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, "Europe/London", 2026);

        Assert.Equal(new DateOnly(2026, 6, 16), boundaries.Today);
    }

    [Fact]
    public void Resolve_OnJanuaryFirst_YearStartEqualsToday()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 30, 0, TimeSpan.Zero));

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, "UTC", 2026);

        Assert.Equal(new DateOnly(2026, 1, 1), boundaries.Today);
        Assert.Equal(new DateOnly(2026, 1, 1), boundaries.YearStart);
    }

    [Fact]
    public void Resolve_Last7DaysStart_IsSixDaysBeforeToday()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, "UTC", 2026);

        Assert.Equal(new DateOnly(2026, 6, 9), boundaries.Last7DaysStart);
    }

    [Fact]
    public void Resolve_WithUnknownTimeZone_ThrowsTimeZoneNotFoundException()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Assert.Throws<TimeZoneNotFoundException>(() => RegenerationRunBoundaries.Resolve(timeProvider, "Not/A_Real_Zone", 2026));
    }

    [Fact]
    public void Resolve_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RegenerationRunBoundaries.Resolve(null!, "UTC", 2026));
    }

    [Fact]
    public void Resolve_WithNullLocalTimeZone_ThrowsArgumentNullException()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => RegenerationRunBoundaries.Resolve(timeProvider, null!, 2026));
    }

    [Fact]
    public void Resolve_ForThePastYear_CoversTheWholeYear()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, "UTC", 2025);

        Assert.Equal(new DateOnly(2025, 1, 1), boundaries.YearStart);
        Assert.Equal(new DateOnly(2025, 12, 31), boundaries.Today);
    }

    [Fact]
    public void Resolve_ForThePastYear_LeavesLast7DaysStartAfterToday_SoTheLastSevenDaysRangeIsEmpty()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, "UTC", 2025);

        Assert.Equal(new DateOnly(2026, 6, 9), boundaries.Last7DaysStart);
        Assert.True(boundaries.Last7DaysStart > boundaries.Today);
    }

    [Fact]
    public void Resolve_ForTheCurrentYear_EndsAtTheLocalToday()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, "UTC", 2026);

        Assert.Equal(new DateOnly(2026, 1, 1), boundaries.YearStart);
        Assert.Equal(new DateOnly(2026, 6, 15), boundaries.Today);
    }

    [Fact]
    public void Resolve_ForAYearAfterTheLocalToday_ThrowsArgumentOutOfRangeException()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(() => RegenerationRunBoundaries.Resolve(timeProvider, "UTC", 2027));
    }

    [Fact]
    public void Resolve_NearNewYear_UsesTheLocalYearToJudgeWhetherTheRequestedYearIsInTheFuture()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero));

        var boundaries = RegenerationRunBoundaries.Resolve(timeProvider, "Pacific/Auckland", 2027);

        Assert.Equal(new DateOnly(2027, 1, 1), boundaries.Today);
    }
}
