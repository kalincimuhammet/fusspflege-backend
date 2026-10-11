using Fusspflege.Core.Entities;
using Fusspflege.Core.Enums;
using Fusspflege.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Infrastructure.Services;

public enum AdminActionResult { Success, NotFound, InvalidState, NotYetHappened }

public record AdminCancelOutcome(AdminActionResult Result, CancelledNotice? Notice = null);

public class AdminAppointmentService
{
    private readonly AppDbContext _db;

    public AdminAppointmentService(AppDbContext db) => _db = db;

    // Admin darf jederzeit absagen, auch kurzfristig (die 24-Stunden-Frist gilt nur für Kunden)
    public async Task<AdminCancelOutcome> CancelAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.Appointments
            .Include(x => x.Customer)
            .Include(x => x.Treatment)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (a is null)
            return new AdminCancelOutcome(AdminActionResult.NotFound);

        if (a.Status is not (AppointmentStatus.Confirmed or AppointmentStatus.Reminded))
            return new AdminCancelOutcome(AdminActionResult.InvalidState);

        var now = DateTime.UtcNow;
        var oldStatus = a.Status;

        a.Status = AppointmentStatus.Cancelled;
        a.CancelledAtUtc = now;
        AddLog(a.Id, oldStatus, AppointmentStatus.Cancelled, now);

        await _db.SaveChangesAsync(ct);

        return new AdminCancelOutcome(
            AdminActionResult.Success,
            new CancelledNotice(a.Customer.Email, a.Customer.FirstName, a.Treatment.Name, a.StartUtc));
    }

    // Erledigt kann ein Termin erst sein, wenn er begonnen hat
    public async Task<AdminActionResult> CompleteAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.Appointments.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (a is null)
            return AdminActionResult.NotFound;

        if (a.Status is not (AppointmentStatus.Confirmed or AppointmentStatus.Reminded))
            return AdminActionResult.InvalidState;

        var now = DateTime.UtcNow;
        if (a.StartUtc > now)
            return AdminActionResult.NotYetHappened;

        var oldStatus = a.Status;
        a.Status = AppointmentStatus.Completed;
        AddLog(a.Id, oldStatus, AppointmentStatus.Completed, now);

        await _db.SaveChangesAsync(ct);
        return AdminActionResult.Success;
    }

    private void AddLog(int appointmentId, AppointmentStatus oldStatus, AppointmentStatus newStatus, DateTime now) =>
        _db.AppointmentLogs.Add(new AppointmentLog
        {
            AppointmentId = appointmentId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAtUtc = now,
            Source = ChangeSource.Admin
        });
}