using Fusspflege.Core.Entities;
using Fusspflege.Core.Services;

namespace Fusspflege.Tests;

public class SlotCalculatorTests
{
    // 5. Januar 2026 ist ein Montag, Berlin = UTC+1 (Winterzeit)
    private static readonly DateOnly Monday = new(2026, 1, 5);
    private static readonly DateTime LongAgo = new(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly SlotCalculator _calc = new();

    private static DateTime Utc(int y, int m, int d, int h, int min) =>
        new(y, m, d, h, min, 0, DateTimeKind.Utc);

    private static List<WorkingHours> MondayMorning() => new()
    {
        new WorkingHours
        {
            Weekday = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0)
        }
    };

    private IReadOnlyList<TimeSlot> Calc(
        DateOnly date,
        int duration = 60,
        List<TimeOff>? timeOffs = null,
        List<BusyRange>? busy = null,
        DateTime? now = null,
        int buffer = 0)
        => _calc.GetFreeSlots(
            date,
            duration,
            MondayMorning(),
            timeOffs ?? new List<TimeOff>(),
            busy ?? new List<BusyRange>(),
            now ?? LongAgo,
            buffer);

    [Fact]
    public void Day_without_working_hours_has_no_slots()
    {
        var sunday = new DateOnly(2026, 1, 4);

        Assert.Empty(Calc(sunday));
    }

    [Fact]
    public void Slots_are_generated_every_30_minutes_and_must_fit_into_working_hours()
    {
        var slots = Calc(Monday, duration: 60);

        // 09:00, 09:30, 10:00, 10:30, 11:00 (Berliner Zeit)
        Assert.Equal(5, slots.Count);
        Assert.Equal(Utc(2026, 1, 5, 8, 0), slots[0].StartUtc);
        Assert.Equal(Utc(2026, 1, 5, 10, 0), slots[^1].StartUtc);
    }

    [Fact]
    public void Booked_appointment_blocks_overlapping_slots()
    {
        // Gebucht 10:00-11:00 Berliner Zeit
        var busy = new List<BusyRange> { new(Utc(2026, 1, 5, 9, 0), Utc(2026, 1, 5, 10, 0)) };

        var slots = Calc(Monday, duration: 60, busy: busy);

        // Übrig bleiben 09:00 und 11:00 Berliner Zeit
        Assert.Equal(2, slots.Count);
        Assert.Equal(Utc(2026, 1, 5, 8, 0), slots[0].StartUtc);
        Assert.Equal(Utc(2026, 1, 5, 10, 0), slots[1].StartUtc);
    }

    [Fact]
    public void Time_off_removes_all_slots_of_that_day()
    {
        var timeOffs = new List<TimeOff>
        {
            new() { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 10) }
        };

        Assert.Empty(Calc(Monday, timeOffs: timeOffs));
    }

    [Fact]
    public void Slots_in_the_past_are_not_offered()
    {
        // Jetzt = 10:15 Berliner Zeit
        var now = Utc(2026, 1, 5, 9, 15);

        var slots = Calc(Monday, duration: 60, now: now);

        // Nur 10:30 und 11:00 Berliner Zeit sind noch in der Zukunft
        Assert.Equal(2, slots.Count);
        Assert.Equal(Utc(2026, 1, 5, 9, 30), slots[0].StartUtc);
    }

    [Fact]
    public void Buffer_keeps_distance_to_existing_appointments()
    {
        // Gebucht 09:00-10:00 Berliner Zeit, 30 Minuten Puffer
        var busy = new List<BusyRange> { new(Utc(2026, 1, 5, 8, 0), Utc(2026, 1, 5, 9, 0)) };

        var slots = Calc(Monday, duration: 60, busy: busy, buffer: 30);

        // Frühester freier Start: 10:30 Berliner Zeit
        Assert.Equal(2, slots.Count);
        Assert.Equal(Utc(2026, 1, 5, 9, 30), slots[0].StartUtc);
    }

    [Fact]
    public void Summer_time_is_converted_correctly()
    {
        // 6. Juli 2026 ist ein Montag, Berlin = UTC+2
        var slots = Calc(new DateOnly(2026, 7, 6), duration: 60);

        Assert.Equal(Utc(2026, 7, 6, 7, 0), slots[0].StartUtc);
    }
}