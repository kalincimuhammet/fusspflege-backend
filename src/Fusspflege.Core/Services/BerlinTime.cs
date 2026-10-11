namespace Fusspflege.Core.Services;

public static class BerlinTime
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    public static DateOnly Today(DateTime nowUtc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Tz));

    // Vom Beginn von fromDate bis zum Ende von toDate (Berliner Zeit) als UTC-Bereich [Start, Ende)
    public static (DateTime StartUtc, DateTime EndUtc) RangeUtc(DateOnly fromDate, DateOnly toDate) =>
        (TimeZoneInfo.ConvertTimeToUtc(fromDate.ToDateTime(TimeOnly.MinValue), Tz),
         TimeZoneInfo.ConvertTimeToUtc(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), Tz));
}