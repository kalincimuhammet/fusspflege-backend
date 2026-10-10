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
    private readonly BookingEmailService _emails;

    public AppointmentsController(BookingService booking, BookingEmailService emails)
    {
        _booking = booking;
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

        return StatusCode(StatusCodes.Status201Created,
            new AppointmentCreatedResponse(r.AppointmentId, r.StartUtc, r.EndUtc, r.TreatmentName));
    }
}