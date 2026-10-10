using Fusspflege.Core.Entities;
using Fusspflege.Core.Enums;
using Fusspflege.Core.Services;
using Fusspflege.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Infrastructure.Services;

public enum BookingError { None, TreatmentNotFound, SlotNotAvailable }

public record BookingRequest(
    int TreatmentId,
    DateTime StartUtc,
    string FirstName,
    string LastName,
    string Email,
    string Street,
    string PostalCode,
    string City);

public record BookingResult(
    int AppointmentId,
    string CancelToken,
    DateTime StartUtc,
    DateTime EndUtc,
    string TreatmentName,
    string CustomerFirstName,
    string CustomerEmail);

public record BookingOutcome(BookingResult? Result, BookingError Error)
{
    public static BookingOutcome Success(BookingResult result) => new(result, BookingError.None);
    public static BookingOutcome Fail(BookingError error) => new(null, error);
}

public class BookingService
{
    private const int MaxDaysAhead = 90;

    private readonly AppDbContext _db;
    private readonly SlotCalculator _slots;

    public BookingService(AppDbContext db, SlotCalculator slots)
    {
        _db = db;
        _slots = slots;
    }

    public async Task<IReadOnlyList<TimeSlot>> GetFreeSlotsAsync(
        int treatmentId, DateOnly date, CancellationToken ct = default)
    {
        var treatment = await _db.Treatments.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == treatmentId && t.IsActive, ct);
        if (treatment is null)
            return Array.Empty<TimeSlot>();

        var now = DateTime.UtcNow;
        if (date > DateOnly.FromDateTime(now).AddDays(MaxDaysAhead))
            return Array.Empty<TimeSlot>();

        var workingHours = await _db.WorkingHours.AsNoTracking()
            .Where(w => w.Weekday == date.DayOfWeek)
            .ToListAsync(ct);

        var timeOffs = await _db.TimeOffs.AsNoTracking()
            .Where(t => t.StartDate <= date && t.EndDate >= date)
            .ToListAsync(ct);

        // Großzügiges UTC-Fenster um den Tag, die Slot-Logik prüft die genauen Überschneidungen
        var windowStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-1);
        var windowEnd = windowStart.AddDays(3);

        var busy = await _db.Appointments.AsNoTracking()
            .Where(a => a.Status != AppointmentStatus.Cancelled
                        && a.StartUtc >= windowStart && a.StartUtc < windowEnd)
            .Select(a => new BusyRange(a.StartUtc, a.EndUtc))
            .ToListAsync(ct);

        return _slots.GetFreeSlots(date, treatment.DurationMinutes, workingHours, timeOffs, busy, now);
    }

    public async Task<BookingOutcome> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        var treatment = await _db.Treatments
            .FirstOrDefaultAsync(t => t.Id == request.TreatmentId && t.IsActive, ct);
        if (treatment is null)
            return BookingOutcome.Fail(BookingError.TreatmentNotFound);

        var startUtc = request.StartUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc)
            : request.StartUtc.ToUniversalTime();

        // Der Slot muss wirklich noch frei sein, die Zeit wird nie blind vom Client übernommen
        var date = _slots.ToLocalDate(startUtc);
        var freeSlots = await GetFreeSlotsAsync(treatment.Id, date, ct);
        var slot = freeSlots.FirstOrDefault(s => s.StartUtc == startUtc);
        if (slot is null)
            return BookingOutcome.Fail(BookingError.SlotNotAvailable);

        var now = DateTime.UtcNow;

        // Kunden über die E-Mail-Adresse wiederverwenden oder neu anlegen
        var email = request.Email.Trim().ToLowerInvariant();
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email, ct);
        if (customer is null)
        {
            customer = new Customer { Email = email };
            _db.Customers.Add(customer);
        }
        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Street = request.Street.Trim();
        customer.PostalCode = request.PostalCode.Trim();
        customer.City = request.City.Trim();
        customer.ConsentAtUtc = now;

        var token = CancelToken.Generate();

        var appointment = new Appointment
        {
            Customer = customer,
            TreatmentId = treatment.Id,
            StartUtc = slot.StartUtc,
            EndUtc = slot.EndUtc,
            Status = AppointmentStatus.Confirmed,
            CancelTokenHash = CancelToken.Hash(token),
            CreatedAtUtc = now
        };

        appointment.Logs.Add(new AppointmentLog
        {
            OldStatus = null,
            NewStatus = AppointmentStatus.Confirmed,
            ChangedAtUtc = now,
            Source = ChangeSource.Customer
        });

        _db.Appointments.Add(appointment);

        try
        {
            // Ein SaveChanges = eine Transaktion: Kunde, Termin und Log werden zusammen gespeichert
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Jemand war schneller, der Unique-Index auf StartUtc hat die Doppelbuchung verhindert
            return BookingOutcome.Fail(BookingError.SlotNotAvailable);
        }

        return BookingOutcome.Success(new BookingResult(
            appointment.Id,
            token,
            appointment.StartUtc,
            appointment.EndUtc,
            treatment.Name,
            customer.FirstName,
            customer.Email));
    }
}