using Fusspflege.Core.Entities;
using Fusspflege.Core.Enums;
using Fusspflege.Core.Services;
using Fusspflege.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Infrastructure.Services;

public enum CancelResult { Success, NotFound, AlreadyCancelled, TooLate, NotCancellable }

public record AppointmentView(
    DateTime StartUtc, DateTime EndUtc, string TreatmentName, AppointmentStatus Status, bool CanCancel);

public record CancelledNotice(string Email, string FirstName, string TreatmentName, DateTime StartUtc);

public record CancelOutcome(CancelResult Result, CancelledNotice? Notice = null);

public class AppointmentService
{
    // Online-Absage nur bis zu dieser Frist vor dem Termin, danach bitte direkt Kontakt aufnehmen
    public const int CancellationDeadlineHours = 24;

    private readonly AppDbContext _db;

    public AppointmentService(AppDbContext db) => _db = db;

    public async Task<AppointmentView?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        if (!IsPlausible(token))
            return null;

        var hash = CancelToken.Hash(token);
        var a = await _db.Appointments.AsNoTracking()
            .Include(x => x.Treatment)
            .FirstOrDefaultAsync(x => x.CancelTokenHash == hash, ct);

        if (a is null)
            return null;

        return new AppointmentView(
            a.StartUtc, a.EndUtc, a.Treatment.Name, a.Status, IsCancellable(a, DateTime.UtcNow));
    }

    public async Task<CancelOutcome> CancelByTokenAsync(string token, CancellationToken ct = default)
    {
        if (!IsPlausible(token))
            return new CancelOutcome(CancelResult.NotFound);

        var hash = CancelToken.Hash(token);
        var a = await _db.Appointments
            .Include(x => x.Customer)
            .Include(x => x.Treatment)
            .FirstOrDefaultAsync(x => x.CancelTokenHash == hash, ct);

        if (a is null)
            return new CancelOutcome(CancelResult.NotFound);

        if (a.Status == AppointmentStatus.Cancelled)
            return new CancelOutcome(CancelResult.AlreadyCancelled);

        if (a.Status is not (AppointmentStatus.Confirmed or AppointmentStatus.Reminded))
            return new CancelOutcome(CancelResult.NotCancellable);

        var now = DateTime.UtcNow;
        if (a.StartUtc - now < TimeSpan.FromHours(CancellationDeadlineHours))
            return new CancelOutcome(CancelResult.TooLate);

        var oldStatus = a.Status;
        a.Status = AppointmentStatus.Cancelled;
        a.CancelledAtUtc = now;

        _db.AppointmentLogs.Add(new AppointmentLog
        {
            AppointmentId = a.Id,
            OldStatus = oldStatus,
            NewStatus = AppointmentStatus.Cancelled,
            ChangedAtUtc = now,
            Source = ChangeSource.Customer
        });

        // Status und Logeintrag werden in einer Transaktion gespeichert
        await _db.SaveChangesAsync(ct);

        return new CancelOutcome(
            CancelResult.Success,
            new CancelledNotice(a.Customer.Email, a.Customer.FirstName, a.Treatment.Name, a.StartUtc));
    }

    private static bool IsPlausible(string token) =>
        !string.IsNullOrWhiteSpace(token) && token.Length <= 100;

    private static bool IsCancellable(Appointment a, DateTime nowUtc) =>
        a.Status is AppointmentStatus.Confirmed or AppointmentStatus.Reminded
        && a.StartUtc - nowUtc >= TimeSpan.FromHours(CancellationDeadlineHours);
}