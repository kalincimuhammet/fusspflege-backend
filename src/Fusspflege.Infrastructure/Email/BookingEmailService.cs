using System.Text;
using Fusspflege.Core.Interfaces;
using Fusspflege.Core.Services;
using Fusspflege.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fusspflege.Infrastructure.Email;

public class BookingEmailService
{
    private readonly IEmailSender _sender;
    private readonly EmailOptions _options;
    private readonly ILogger<BookingEmailService> _logger;

    public BookingEmailService(
        IEmailSender sender,
        IOptions<EmailOptions> options,
        ILogger<BookingEmailService> logger)
    {
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendConfirmationAsync(BookingResult booking, CancellationToken ct = default)
    {
        try
        {
            var cancelUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/cancel/{booking.CancelToken}";
            var (subject, html, text) = EmailTemplates.Confirmation(booking, _options.BusinessName, cancelUrl);

            var ics = IcsBuilder.Build(
                uid: $"termin-{booking.AppointmentId}@fusspflege",
                summary: $"{booking.TreatmentName} ({_options.BusinessName})",
                description: $"Ihr Termin: {booking.TreatmentName}",
                location: "Hausbesuch",
                startUtc: booking.StartUtc,
                endUtc: booking.EndUtc,
                nowUtc: DateTime.UtcNow);

            var attachment = new OutgoingAttachment("termin.ics", "text/calendar", Encoding.UTF8.GetBytes(ics));

            await _sender.SendAsync(
                new OutgoingEmail(booking.CustomerEmail, booking.CustomerFirstName, subject, html, text, attachment),
                ct);
        }
        catch (Exception ex)
        {
            // Der Termin ist schon gespeichert, ein Mailfehler darf die Buchung nicht scheitern lassen
            _logger.LogError(ex,
                "Bestätigungsmail für Termin {AppointmentId} konnte nicht gesendet werden", booking.AppointmentId);
        }
    }
}