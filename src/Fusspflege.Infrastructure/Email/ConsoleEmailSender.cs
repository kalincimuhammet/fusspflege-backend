using Fusspflege.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fusspflege.Infrastructure.Email;

// Für die Entwicklung: schreibt die Mail ins Log, statt sie zu versenden
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

    public Task SendAsync(OutgoingEmail email, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "E-MAIL (Console) an {To} | Betreff: {Subject} | Anhang: {Attachment}\n{Body}",
            email.ToAddress,
            email.Subject,
            email.Attachment?.FileName ?? "keiner",
            email.PlainTextBody);

        return Task.CompletedTask;
    }
}