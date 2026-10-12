using Fusspflege.Api.Dtos;
using Fusspflege.Infrastructure.Email;
using Fusspflege.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fusspflege.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly BookingService _booking;
    private readonly AppointmentService _appointments;
    private readonly BookingEmailService _emails;

    public AppointmentsController(
        BookingService booking, AppointmentService appointments, BookingEmailService emails)
    {
        _booking = booking;
        _appointments = appointments;
        _emails = emails;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAppointmentRequest request, CancellationToken ct)
    {
        var outcome = await _booking.BookAsync(new BookingRequest(
            request.TreatmentId,
            request.StartUtc,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Street,
            request.PostalCode,
            request.City), ct);

        if (outcome.Error == BookingError.TreatmentNotFound)
            return Problem(statusCode: 404, title: "Behandlung nicht gefunden.");

        if (outcome.Error == BookingError.SlotNotAvailable)
            return Problem(statusCode: 409, title: "Dieser Termin ist leider nicht mehr verfügbar.");

        var r = outcome.Result!;

        await _emails.SendConfirmationAsync(r, ct);

        // Die Praxis bekommt ebenfalls eine Nachricht (nur wenn eine Adresse konfiguriert ist)
        var customer = new CustomerContact(
            $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            request.Email.Trim(),
            request.Street.Trim(),
            request.PostalCode.Trim(),
            request.City.Trim());
        await _emails.SendAdminBookingNotificationAsync(r, customer, ct);

        return StatusCode(StatusCodes.Status201Created,
            new AppointmentCreatedResponse(r.AppointmentId, r.StartUtc, r.EndUtc, r.TreatmentName));
    }

    [HttpGet("by-token/{token}")]
    public async Task<IActionResult> GetByToken(string token, CancellationToken ct)
    {
        var view = await _appointments.GetByTokenAsync(token, ct);
        if (view is null)
            return Problem(statusCode: 404, title: "Termin nicht gefunden.");

        return Ok(new AppointmentDetailsDto(
            view.StartUtc, view.EndUtc, view.TreatmentName, view.Status.ToString(), view.CanCancel));
    }

    // POST statt GET: Mail-Scanner und Link-Vorschauen rufen Links auf und würden sonst versehentlich stornieren
    [HttpPost("by-token/{token}/cancel")]
    public async Task<IActionResult> Cancel(string token, CancellationToken ct)
    {
        var outcome = await _appointments.CancelByTokenAsync(token, ct);

        if (outcome.Result == CancelResult.NotFound)
            return Problem(statusCode: 404, title: "Termin nicht gefunden.");

        if (outcome.Result == CancelResult.AlreadyCancelled)
            return Problem(statusCode: 409, title: "Dieser Termin wurde bereits abgesagt.");

        if (outcome.Result == CancelResult.TooLate)
            return Problem(statusCode: 409,
                title: $"Online-Absagen sind nur bis {AppointmentService.CancellationDeadlineHours} Stunden vor dem Termin möglich. Bitte kontaktieren Sie uns direkt.");

        if (outcome.Result == CancelResult.NotCancellable)
            return Problem(statusCode: 409, title: "Dieser Termin kann nicht mehr abgesagt werden.");

        var n = outcome.Notice!;
        await _emails.SendCancellationAsync(n.Email, n.FirstName, n.TreatmentName, n.StartUtc, ct);
        await _emails.SendAdminCancellationNotificationAsync(n, ct);

        return NoContent();
    }
}