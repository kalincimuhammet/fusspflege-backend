using System.Globalization;
using Fusspflege.Api.Dtos;
using Fusspflege.Core;
using Fusspflege.Core.Entities;
using Fusspflege.Core.Enums;
using Fusspflege.Core.Services;
using Fusspflege.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminScheduleController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminScheduleController(AppDbContext db) => _db = db;

    // ----- Arbeitszeiten -----

    [HttpGet("working-hours")]
    public async Task<IActionResult> GetWorkingHours(CancellationToken ct)
    {
        var rows = await _db.WorkingHours.AsNoTracking().ToListAsync(ct);

        // Montag zuerst, Sonntag zuletzt
        var result = rows
            .OrderBy(w => ((int)w.Weekday + 6) % 7)
            .ThenBy(w => w.StartTime)
            .Select(w => new WorkingHoursDto(
                w.Weekday,
                w.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                w.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture)));

        return Ok(result);
    }

    // Ersetzt die komplette Wochenplanung auf einmal, in einer Transaktion.
    // Bereits gebuchte Termine bleiben unberührt, es ändern sich nur künftig angebotene Zeiten.
    [HttpPut("working-hours")]
    public async Task<IActionResult> ReplaceWorkingHours(List<WorkingHoursDto> items, CancellationToken ct)
    {
        var parsed = new List<WorkingHours>();

        foreach (var item in items)
        {
            if (!Enum.IsDefined(item.Weekday))
                return Problem(statusCode: 400, title: "Ungültiger Wochentag.");

            if (!TimeOnly.TryParseExact(item.StartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
                || !TimeOnly.TryParseExact(item.EndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                return Problem(statusCode: 400, title: "Zeiten müssen im Format HH:mm angegeben werden.");

            if (start >= end)
                return Problem(statusCode: 400, title: "Die Endzeit muss nach der Startzeit liegen.");

            parsed.Add(new WorkingHours { Weekday = item.Weekday, StartTime = start, EndTime = end });
        }

        // Zeitblöcke am selben Wochentag (z. B. vormittags und nachmittags) dürfen sich nicht überschneiden
        foreach (var day in parsed.GroupBy(p => p.Weekday))
        {
            var ordered = day.OrderBy(p => p.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                    return Problem(statusCode: 400, title: "Zeitblöcke am selben Wochentag dürfen sich nicht überschneiden.");
            }
        }

        _db.WorkingHours.RemoveRange(await _db.WorkingHours.ToListAsync(ct));
        _db.WorkingHours.AddRange(parsed);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ----- Urlaub und freie Tage -----

    // Nur laufende und künftige Einträge
    [HttpGet("time-off")]
    public async Task<IActionResult> GetTimeOff(CancellationToken ct)
    {
        var today = BerlinTime.Today(DateTime.UtcNow);

        var items = await _db.TimeOffs.AsNoTracking()
            .Where(t => t.EndDate >= today)
            .OrderBy(t => t.StartDate)
            .Select(t => new TimeOffDto(t.Id, t.StartDate, t.EndDate, t.Reason))
            .ToListAsync(ct);

        return Ok(items);
    }

    // Gibt zusätzlich an, wie viele bereits gebuchte Termine in den Zeitraum fallen,
    // denn die werden nicht automatisch abgesagt
    [HttpPost("time-off")]
    public async Task<IActionResult> CreateTimeOff(TimeOffRequest request, CancellationToken ct)
    {
        if (request.EndDate < request.StartDate)
            return Problem(statusCode: 400, title: "Das Enddatum darf nicht vor dem Startdatum liegen.");

        if (request.EndDate < BerlinTime.Today(DateTime.UtcNow))
            return Problem(statusCode: 400, title: "Der Zeitraum liegt in der Vergangenheit.");

        if (request.EndDate.DayNumber - request.StartDate.DayNumber > 366)
            return Problem(statusCode: 400, title: "Der Zeitraum darf höchstens ein Jahr umfassen.");

        var timeOff = new TimeOff
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim()
        };

        _db.TimeOffs.Add(timeOff);
        await _db.SaveChangesAsync(ct);

        var (fromUtc, toUtc) = BerlinTime.RangeUtc(request.StartDate, request.EndDate);
        var conflicts = await _db.Appointments.CountAsync(a =>
            (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Reminded)
            && a.StartUtc >= fromUtc && a.StartUtc < toUtc, ct);

        var dto = new TimeOffDto(timeOff.Id, timeOff.StartDate, timeOff.EndDate, timeOff.Reason);
        return Created($"/api/admin/time-off/{timeOff.Id}", new TimeOffCreatedDto(dto, conflicts));
    }

    [HttpDelete("time-off/{id:int}")]
    public async Task<IActionResult> DeleteTimeOff(int id, CancellationToken ct)
    {
        var timeOff = await _db.TimeOffs.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (timeOff is null)
            return Problem(statusCode: 404, title: "Eintrag nicht gefunden.");

        _db.TimeOffs.Remove(timeOff);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}