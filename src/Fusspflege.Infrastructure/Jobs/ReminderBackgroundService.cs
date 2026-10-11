using Fusspflege.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fusspflege.Infrastructure.Jobs;

// Läuft einmal täglich. Ein häufigerer Takt würde die Serverless-Datenbank am Pausieren hindern und Kosten verursachen.
public class ReminderBackgroundService : BackgroundService
{
    private const int MaxAttempts = 3;

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceScopeFactory scopes, ILogger<ReminderBackgroundService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var wait = ReminderSchedule.TimeUntilNextRun(DateTime.UtcNow);
                _logger.LogInformation("Nächster Erinnerungslauf in {Wait}", wait);

                await Task.Delay(wait, stoppingToken);
                await RunWithRetryAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Die Anwendung wird beendet
        }
    }

    // Die Datenbank kann nach einer Pause erst verzögert antworten, deshalb bis zu drei Versuche
    private async Task RunWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();

                var sent = await reminders.RunOnceAsync(DateTime.UtcNow, ct);
                _logger.LogInformation("Erinnerungslauf beendet, {Count} Erinnerung(en) versendet", sent);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erinnerungslauf fehlgeschlagen (Versuch {Attempt} von {Max})", attempt, MaxAttempts);

                if (attempt < MaxAttempts)
                    await Task.Delay(TimeSpan.FromMinutes(2), ct);
            }
        }
    }
}