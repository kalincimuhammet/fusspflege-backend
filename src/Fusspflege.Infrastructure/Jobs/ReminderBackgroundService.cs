using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fusspflege.Infrastructure.Jobs;

public class ReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

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
            // Kurze Pause, damit die App beim Start erst vollständig hochfährt
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                await RunSafelyAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Die Anwendung wird beendet
        }
    }

    private async Task RunSafelyAsync(CancellationToken ct)
    {
        try
        {
            // Pro Durchlauf ein eigener Scope, weil der DbContext kurzlebig ist
            using var scope = _scopes.CreateScope();
            var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();

            var sent = await reminders.RunOnceAsync(DateTime.UtcNow, ct);
            if (sent > 0)
                _logger.LogInformation("{Count} Erinnerung(en) versendet", sent);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Fehler im Erinnerungsjob");
        }
    }
}