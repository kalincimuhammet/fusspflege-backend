using Fusspflege.Api.Dtos;
using Fusspflege.Core;
using Fusspflege.Core.Enums;
using Fusspflege.Core.Services;
using Fusspflege.Infrastructure.Data;
using Fusspflege.Infrastructure.Email;
using Fusspflege.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Api.Controllers;

[ApiController]
[Route("api/admin/appointments")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminAppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AdminAppointmentService _service;
    private readonly BookingEmailService _emails;

    public AdminAppointmentsController(
        AppDbContext db, AdminAppointmentService service, BookingEmailService emails)
    {
        _db = db;
        _service = service;
        _emails = emails;
    }

    // Lesen: direkt per Projektion in die Antwort. Ohne Angaben: heute und die nächsten 30 Tage.
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] AppointmentStatus? status,
        CancellationToken ct)
    {
        var fromDate = from ?? BerlinTime.Today(DateTime.UtcNow);
        var toDate = to ?? fromDate.AddDays(30);

        if (toDate < fromDate)
            return Problem(statusCode: 400, title: "Das Enddatum darf nicht vor dem Startdatum liegen.");

        if (toDate.DayNumber - fromDate.DayNumber > 366)
            return Problem(statusCode: 400, title: "Der Zeitraum darf höchstens ein Jahr umfassen.");

        var (fromUtc, toUtc) = BerlinTime.RangeUtc(fromDate, toDate);

        var query = _db.Appointments.AsNoTracking()
            .Where(a => a.StartUtc >= fromUtc && a.StartUtc < toUtc);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var rows = await query
            .OrderBy(a => a.StartUtc)
            .Select(a => new
            {
                a.Id,
                a.StartUtc,
                a.EndUtc,
                a.Status,
                TreatmentName = a.Treatment.Name,
                a.Customer.FirstName,
                a.Customer.LastName,
                a.Customer.Email,
                a.Customer.Street,
                a.Customer.PostalCode,
                a.Customer.City,
                a.CreatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(rows.Select(r => new AdminAppointmentDto(
            r.Id, r.StartUtc, r.EndUtc, r.Status.ToString(), r.TreatmentName,
            $"{r.FirstName} {r.LastName}", r.Email, r.Street, r.PostalCode, r.City, r.CreatedAtUtc)));
    }

    // Verlauf eines Termins: wer hat wann welchen Statuswechsel ausgelöst
    [HttpGet("{id:int}/log")]
    public async Task<IActionResult> Log(int id, CancellationToken ct)
    {
        if (!await _db.Appointments.AnyAsync(a => a.Id == id, ct))
            return Problem(statusCode: 404, title: "Termin nicht gefunden.");

        var rows = await _db.AppointmentLogs.AsNoTracking()
            .Where(l => l.AppointmentId == id)
            .OrderBy(l => l.ChangedAtUtc).ThenBy(l => l.Id)
            .Select(l => new { l.OldStatus, l.NewStatus, l.ChangedAtUtc, l.Source })
            .ToListAsync(ct);

        return Ok(rows.Select(r => new AppointmentLogDto(
            r.OldStatus?.ToString(), r.NewStatus.ToString(), r.ChangedAtUtc, r.Source.ToString())));
    }

    // Schreiben: über den Service, der die Regeln prüft und den Verlauf schreibt
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var outcome = await _service.CancelAsync(id, ct);

        if (outcome.Result == AdminActionResult.NotFound)
            return Problem(statusCode: 404, title: "Termin nicht gefunden.");

        if (outcome.Result != AdminActionResult.Success)
            return Problem(statusCode: 409, title: "Dieser Termin kann nicht mehr abgesagt werden.");

        // Der Kunde bekommt die gleiche Absagemail wie bei einer eigenen Stornierung
        var n = outcome.Notice!;
        await _emails.SendCancellationAsync(n.Email, n.FirstName, n.TreatmentName, n.StartUtc, ct);

        return NoContent();
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var result = await _service.CompleteAsync(id, ct);

        return result switch
        {
            AdminActionResult.Success => NoContent(),
            AdminActionResult.NotFound => Problem(statusCode: 404, title: "Termin nicht gefunden."),
            AdminActionResult.NotYetHappened => Problem(statusCode: 409, title: "Der Termin hat noch nicht begonnen."),
            _ => Problem(statusCode: 409, title: "Dieser Termin kann nicht als erledigt markiert werden.")
        };
    }
}