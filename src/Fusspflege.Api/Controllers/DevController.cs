using Fusspflege.Infrastructure.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Fusspflege.Api.Controllers;

[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    [HttpPost("run-reminders")]
    public async Task<IActionResult> RunReminders(
        [FromQuery] DateTime? nowUtc,
        [FromServices] ReminderService reminders,
        [FromServices] IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        // Mit nowUtc lässt sich ein anderer Zeitpunkt simulieren
        var now = (nowUtc ?? DateTime.UtcNow).ToUniversalTime();
        var sent = await reminders.RunOnceAsync(now, ct);

        return Ok(new { sent });
    }
}