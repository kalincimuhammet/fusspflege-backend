using Fusspflege.Api.Dtos;
using Fusspflege.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fusspflege.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SlotsController : ControllerBase
{
    private readonly BookingService _booking;

    public SlotsController(BookingService booking) => _booking = booking;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SlotDto>>> Get(
        [FromQuery] int treatmentId, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var slots = await _booking.GetFreeSlotsAsync(treatmentId, date, ct);
        return Ok(slots.Select(s => new SlotDto(s.StartUtc, s.EndUtc)));
    }
}