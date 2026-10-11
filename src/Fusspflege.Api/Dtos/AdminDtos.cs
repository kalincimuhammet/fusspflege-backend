using System.ComponentModel.DataAnnotations;

namespace Fusspflege.Api.Dtos;

public record AdminAppointmentDto(
    int Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    string TreatmentName,
    string CustomerName,
    string CustomerEmail,
    string Street,
    string PostalCode,
    string City,
    DateTime CreatedAtUtc);

public record AppointmentLogDto(string? OldStatus, string NewStatus, DateTime ChangedAtUtc, string Source);

public record AdminTreatmentDto(int Id, string Name, int DurationMinutes, int PriceCents, bool IsActive);

public class TreatmentRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(5, 480)]
    public int DurationMinutes { get; set; }

    [Range(0, 100000)]
    public int PriceCents { get; set; }

    public bool IsActive { get; set; } = true;
}

// Weekday: 0 = Sonntag ... 6 = Samstag, Zeiten im Format HH:mm (Berliner Ortszeit)
public record WorkingHoursDto(DayOfWeek Weekday, string StartTime, string EndTime);

public record TimeOffDto(int Id, DateOnly StartDate, DateOnly EndDate, string? Reason);

public class TimeOffRequest
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }
}

public record TimeOffCreatedDto(TimeOffDto TimeOff, int ConflictingAppointments);