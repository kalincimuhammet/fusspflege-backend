using Fusspflege.Core.Services;

namespace Fusspflege.Tests;

public class ReminderScheduleTests
{
    private static DateTime Utc(int y, int m, int d, int h, int min) =>
        new(y, m, d, h, min, 0, DateTimeKind.Utc);

    [Fact]
    public void Before_run_time_the_next_run_is_today()
    {
        // 08:00 Berliner Sommerzeit, nächster Lauf 10:05 = 2 Stunden 5 Minuten später
        var wait = ReminderSchedule.TimeUntilNextRun(Utc(2026, 10, 11, 6, 0));

        Assert.Equal(TimeSpan.FromMinutes(125), wait);
    }

    [Fact]
    public void After_run_time_the_next_run_is_tomorrow()
    {
        // 11:00 Berliner Zeit, nächster Lauf morgen 10:05
        var wait = ReminderSchedule.TimeUntilNextRun(Utc(2026, 10, 11, 9, 0));

        Assert.Equal(TimeSpan.FromMinutes(23 * 60 + 5), wait);
    }

    [Fact]
    public void Exactly_at_run_time_the_next_run_is_tomorrow()
    {
        var wait = ReminderSchedule.TimeUntilNextRun(Utc(2026, 10, 11, 8, 5));

        Assert.Equal(TimeSpan.FromHours(24), wait);
    }

    [Fact]
    public void Winter_time_is_handled()
    {
        // 09:00 Berliner Winterzeit (UTC+1), nächster Lauf in 65 Minuten
        var wait = ReminderSchedule.TimeUntilNextRun(Utc(2026, 1, 5, 8, 0));

        Assert.Equal(TimeSpan.FromMinutes(65), wait);
    }

    [Fact]
    public void Across_the_daylight_saving_change_the_run_stays_at_10_05_local_time()
    {
        // 24.10. 11:00 (Sommerzeit) bis 25.10. 10:05 (Winterzeit): wegen der Umstellung 24 Stunden 5 Minuten
        var wait = ReminderSchedule.TimeUntilNextRun(Utc(2026, 10, 24, 9, 0));

        Assert.Equal(TimeSpan.FromMinutes(24 * 60 + 5), wait);
    }
}