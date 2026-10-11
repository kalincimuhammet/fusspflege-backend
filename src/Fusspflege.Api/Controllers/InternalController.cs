using System.Security.Cryptography;
using System.Text;
using Fusspflege.Infrastructure.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Fusspflege.Api.Controllers;

// Nur für automatische Aufrufe (z. B. GitHub Actions), geschützt durch einen geheimen Schlüssel im Header
[ApiController]
[Route("api/internal")]
public class InternalController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ReminderService _reminders;

    public InternalController(IConfiguration config, ReminderService reminders)
    {
        _config = config;
        _reminders = reminders;
    }

    [HttpPost("run-reminders")]
    public async Task<IActionResult> RunReminders(CancellationToken ct)
    {
        var expected = _config["Reminders:TriggerKey"];

        // Ohne ausreichend langen Schlüssel in der Konfiguration existiert der Endpunkt praktisch nicht
        if (string.IsNullOrWhiteSpace(expected) || expected.Length < 32)
            return NotFound();

        var provided = Request.Headers["X-Api-Key"].ToString();
        if (!FixedTimeEquals(provided, expected))
            return Unauthorized();

        var sent = await _reminders.RunOnceAsync(DateTime.UtcNow, ct);
        return Ok(new { sent });
    }

    // Zeitkonstanter Vergleich, damit sich der Schlüssel nicht über Antwortzeiten erraten lässt
    private static bool FixedTimeEquals(string a, string b)
    {
        var hashA = SHA256.HashData(Encoding.UTF8.GetBytes(a));
        var hashB = SHA256.HashData(Encoding.UTF8.GetBytes(b));
        return CryptographicOperations.FixedTimeEquals(hashA, hashB);
    }
}