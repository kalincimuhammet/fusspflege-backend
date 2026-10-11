using Fusspflege.Api.Dtos;
using Fusspflege.Core;
using Fusspflege.Core.Entities;
using Fusspflege.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Api.Controllers;

[ApiController]
[Route("api/admin/treatments")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminTreatmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminTreatmentsController(AppDbContext db) => _db = db;

    // Anders als die öffentliche Liste auch mit deaktivierten Behandlungen
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.Treatments.AsNoTracking()
            .OrderByDescending(t => t.IsActive).ThenBy(t => t.Name)
            .Select(t => new AdminTreatmentDto(t.Id, t.Name, t.DurationMinutes, t.PriceCents, t.IsActive))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TreatmentRequest request, CancellationToken ct)
    {
        var treatment = new Treatment
        {
            Name = request.Name.Trim(),
            DurationMinutes = request.DurationMinutes,
            PriceCents = request.PriceCents,
            IsActive = request.IsActive
        };

        _db.Treatments.Add(treatment);
        await _db.SaveChangesAsync(ct);

        return Created($"/api/admin/treatments/{treatment.Id}", ToDto(treatment));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TreatmentRequest request, CancellationToken ct)
    {
        var treatment = await _db.Treatments.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (treatment is null)
            return Problem(statusCode: 404, title: "Behandlung nicht gefunden.");

        treatment.Name = request.Name.Trim();
        treatment.DurationMinutes = request.DurationMinutes;
        treatment.PriceCents = request.PriceCents;
        treatment.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(treatment));
    }

    private static AdminTreatmentDto ToDto(Treatment t) =>
        new(t.Id, t.Name, t.DurationMinutes, t.PriceCents, t.IsActive);
}