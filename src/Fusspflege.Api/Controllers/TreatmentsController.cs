using Fusspflege.Api.Dtos;
using Fusspflege.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TreatmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TreatmentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TreatmentDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Treatments.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new TreatmentDto(t.Id, t.Name, t.DurationMinutes, t.PriceCents))
            .ToListAsync(ct);

        return Ok(items);
    }
}