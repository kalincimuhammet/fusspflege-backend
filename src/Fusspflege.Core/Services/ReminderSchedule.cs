namespace Fusspflege.Core.Services;

// Berechnet, wann der Erinnerungsjob das nächste Mal laufen soll: einmal täglich, Berliner Zeit
public static class ReminderSchedule
{
    public static readonly TimeOnly DailyRunTime = new(10, 5);

    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static TimeSpan TimeUntilNextRun(DateTime nowUtc)
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Berlin);
        var nextLocal = nowLocal.Date + DailyRunTime.ToTimeSpan();

        if (nextLocal <= nowLocal)
            nextLocal = nextLocal.AddDays(1);

        // 10:05 Uhr liegt nie in der Zeitumstellung (die passiert nachts), die Umrechnung ist eindeutig
        return TimeZoneInfo.ConvertTimeToUtc(nextLocal, Berlin) - nowUtc;
    }
}