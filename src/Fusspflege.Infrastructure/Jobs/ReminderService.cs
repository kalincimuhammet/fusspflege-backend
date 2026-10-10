using Fusspflege.Core.Entities;
using Fusspflege.Core.Enums;
using Fusspflege.Infrastructure.Data;
using Fusspflege.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fusspflege.Infrastructure.Jobs;

public class ReminderService
{
    // Erinnerungen gehen ab dieser Uhrzeit (Berliner Zeit) für die Termine des Folgetags raus
    private const int SendFromHourLocal = 10;

    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    private readonly AppDbContext _db;
    private readonly BookingEmailService _emails;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(AppDbContext db, BookingEmailService emails, ILogger<ReminderService> logger)
    {
        _db = db;
        _emails = emails;
        _logger = logger;
    }

    // Gibt die Anzahl versendeter Erinnerungen zurück
    public async Task<int> RunOnceAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Berlin);
        if (nowLocal.Hour < SendFromHourLocal)
            return 0;

        var tomorrow = DateOnly.FromDateTime(nowLocal).AddDays(1);

        // Grobe Vorauswahl in SQL, die Prüfung auf das Berliner Datum folgt im Speicher
        var candidates = await _db.Appointments.AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Treatment)
            .Where(a => a.Status == AppointmentStatus.Confirmed
                        && a.ReminderSentAtUtc == null
                        && a.StartUtc > nowUtc
                        && a.StartUtc <= nowUtc.AddHours(48))
            .ToListAsync(ct);

        var sent = 0;

        foreach (var a in candidates)
        {
            ct.ThrowIfCancellationRequested();

            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(a.StartUtc, Berlin));
            if (localDate != tomorrow)
                continue;

            // Wer erst kurz vor dem Termin gebucht hat, braucht keine Erinnerung mehr
            if (a.CreatedAtUtc > a.StartUtc.AddHours(-24))
                continue;

            // Atomar beanspruchen: Nur ein Durchlauf kann diese Zeile von Confirmed auf Reminded setzen.
            // Das macht den Job idempotent, auch wenn er doppelt oder parallel läuft.
            var claimed = await _db.Appointments
                .Where(x => x.Id == a.Id
                            && x.Status == AppointmentStatus.Confirmed
                            && x.ReminderSentAtUtc == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, AppointmentStatus.Reminded)
                    .SetProperty(x => x.ReminderSentAtUtc, (DateTime?)nowUtc), ct);

            if (claimed == 0)
                continue;

            try
            {
                await _emails.SendReminderAsync(
                    a.Customer.Email, a.Customer.FirstName, a.Treatment.Name, a.StartUtc, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erinnerung für Termin {AppointmentId} fehlgeschlagen, neuer Versuch folgt", a.Id);

                // Beanspruchung zurücknehmen, der nächste Durchlauf versucht es erneut
                await _db.Appointments
                    .Where(x => x.Id == a.Id && x.Status == AppointmentStatus.Reminded)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, AppointmentStatus.Confirmed)
                        .SetProperty(x => x.ReminderSentAtUtc, (DateTime?)null), ct);

                continue;
            }

            _db.AppointmentLogs.Add(new AppointmentLog
            {
                AppointmentId = a.Id,
                OldStatus = AppointmentStatus.Confirmed,
                NewStatus = AppointmentStatus.Reminded,
                ChangedAtUtc = nowUtc,
                Source = ChangeSource.System
            });
            await _db.SaveChangesAsync(ct);

            sent++;
        }

        return sent;
    }
}