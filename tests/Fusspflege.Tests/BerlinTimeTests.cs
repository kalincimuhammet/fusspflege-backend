using Fusspflege.Core.Services;

namespace Fusspflege.Tests;

public class BerlinTimeTests
{
    [Fact]
    public void Winter_day_starts_one_hour_before_midnight_utc()
    {
        var (start, end) = BerlinTime.RangeUtc(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5));

        Assert.Equal(new DateTime(2026, 1, 4, 23, 0, 0), start);
        Assert.Equal(new DateTime(2026, 1, 5, 23, 0, 0), end);
    }

    [Fact]
    public void Summer_day_starts_two_hours_before_midnight_utc()
    {
        var (start, end) = BerlinTime.RangeUtc(new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 6));

        Assert.Equal(new DateTime(2026, 7, 5, 22, 0, 0), start);
        Assert.Equal(new DateTime(2026, 7, 6, 22, 0, 0), end);
    }

    [Fact]
    public void Day_of_daylight_saving_change_is_25_hours_long()
    {
        // 25. Oktober 2026: Die Uhr wird von 3 auf 2 Uhr zurückgestellt
        var (start, end) = BerlinTime.RangeUtc(new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 25));

        Assert.Equal(TimeSpan.FromHours(25), end - start);
    }
}