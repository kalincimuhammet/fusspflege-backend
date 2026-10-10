using Fusspflege.Core.Entities;

namespace Fusspflege.Core.Services;

public record TimeSlot(DateTime StartUtc, DateTime EndUtc);

public record BusyRange(DateTime StartUtc, DateTime EndUtc);

public class SlotCalculator
{
    private const int StepMinutes = 30;
    private readonly TimeZoneInfo _tz;

    public SlotCalculator(TimeZoneInfo? timeZone = null)
    {
        _tz = timeZone ?? TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
    }
    // Welches Berliner Datum gehört zu einem UTC-Zeitpunkt?
    public DateOnly ToLocalDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, _tz));
    public IReadOnlyList<TimeSlot> GetFreeSlots(
        DateOnly date,
        int durationMinutes,
        IEnumerable<WorkingHours> workingHours,
        IEnumerable<TimeOff> timeOffs,
        IEnumerable<BusyRange> busyRanges,
        DateTime nowUtc,
        int bufferMinutes = 0)
    {
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        // Urlaub oder freier Tag: keine Slots
        if (timeOffs.Any(t => t.StartDate <= date && date <= t.EndDate))
            return Array.Empty<TimeSlot>();

        var busy = busyRanges.ToList();
        var result = new List<TimeSlot>();
        var duration = TimeSpan.FromMinutes(durationMinutes);
        var buffer = TimeSpan.FromMinutes(bufferMinutes);

        var blocks = workingHours
            .Where(w => w.Weekday == date.DayOfWeek)
            .OrderBy(w => w.StartTime);

        foreach (var block in blocks)
        {
            // Lokale Berliner Zeit (Kind = Unspecified)
            var blockEnd = date.ToDateTime(block.EndTime);

            for (var start = date.ToDateTime(block.StartTime);
                 start + duration <= blockEnd;
                 start = start.AddMinutes(StepMinutes))
            {
                var end = start + duration;

                // Zeiten, die es bei der Umstellung auf Sommerzeit nicht gibt, überspringen
                if (_tz.IsInvalidTime(start) || _tz.IsInvalidTime(end))
                    continue;

                var startUtc = TimeZoneInfo.ConvertTimeToUtc(start, _tz);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(end, _tz);

                // Keine Slots in der Vergangenheit
                if (startUtc <= nowUtc)
                    continue;

                // Überschneidung mit bestehenden Terminen (plus Pufferzeit, z. B. Fahrtzeit)
                var blocked = busy.Any(b =>
                    startUtc < b.EndUtc + buffer && endUtc + buffer > b.StartUtc);

                if (!blocked)
                    result.Add(new TimeSlot(startUtc, endUtc));
            }
        }

        return result.OrderBy(s => s.StartUtc).ToList();
    }
}