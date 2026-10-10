using System.Globalization;
using System.Net;
using Fusspflege.Infrastructure.Services;

namespace Fusspflege.Infrastructure.Email;

public static class EmailTemplates
{
    private static readonly string[] Days =
        { "Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag" };

    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    // Bewusst ohne Kultur-Einstellungen, damit die Ausgabe auf jedem Server gleich deutsch ist
    public static string FormatDateTime(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Berlin);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Days[(int)local.DayOfWeek]}, {local:dd.MM.yyyy} um {local:HH:mm} Uhr");
    }

    public static (string Subject, string Html, string Text) Confirmation(
        BookingResult booking, string businessName, string cancelUrl)
    {
        var when = FormatDateTime(booking.StartUtc);
        var name = WebUtility.HtmlEncode(booking.CustomerFirstName);
        var treatment = WebUtility.HtmlEncode(booking.TreatmentName);
        var business = WebUtility.HtmlEncode(businessName);
        var link = WebUtility.HtmlEncode(cancelUrl);

        var subject = $"Ihre Terminbestätigung – {businessName}";

        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#222;line-height:1.5">
              <h2 style="margin-bottom:4px">Vielen Dank für Ihre Buchung, {name}!</h2>
              <p>Ihr Termin ist bestätigt.</p>
              <table style="border-collapse:collapse;margin:12px 0">
                <tr><td style="padding:4px 16px 4px 0;color:#666">Behandlung</td><td><strong>{treatment}</strong></td></tr>
                <tr><td style="padding:4px 16px 4px 0;color:#666">Termin</td><td><strong>{when}</strong></td></tr>
              </table>
              <p>Eine Kalenderdatei finden Sie im Anhang.</p>
              <p>Sie können nicht? Sie können den Termin hier kostenlos absagen:<br>
                <a href="{link}">Termin absagen</a></p>
              <p style="color:#666;font-size:13px">Viele Grüße<br>{business}</p>
            </div>
            """;

        var text = $"""
            Vielen Dank für Ihre Buchung, {booking.CustomerFirstName}!

            Behandlung: {booking.TreatmentName}
            Termin: {when}

            Eine Kalenderdatei finden Sie im Anhang.

            Termin absagen: {cancelUrl}

            Viele Grüße
            {businessName}
            """;

        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) Reminder(
        string firstName, string treatmentName, DateTime startUtc, string businessName, string contactInfo)
    {
        var when = FormatDateTime(startUtc);
        var name = WebUtility.HtmlEncode(firstName);
        var treatment = WebUtility.HtmlEncode(treatmentName);
        var business = WebUtility.HtmlEncode(businessName);

        var contactHtml = string.IsNullOrWhiteSpace(contactInfo)
            ? ""
            : $"<p>Falls Sie den Termin nicht wahrnehmen können, melden Sie sich bitte rechtzeitig: {WebUtility.HtmlEncode(contactInfo)}</p>";
        var contactText = string.IsNullOrWhiteSpace(contactInfo)
            ? ""
            : $"Falls Sie den Termin nicht wahrnehmen können, melden Sie sich bitte rechtzeitig: {contactInfo}\n\n";

        var subject = $"Erinnerung an Ihren Termin morgen – {businessName}";

        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#222;line-height:1.5">
              <h2 style="margin-bottom:4px">Erinnerung an Ihren Termin morgen</h2>
              <p>Hallo {name}, wir möchten Sie an Ihren Termin erinnern:</p>
              <table style="border-collapse:collapse;margin:12px 0">
                <tr><td style="padding:4px 16px 4px 0;color:#666">Behandlung</td><td><strong>{treatment}</strong></td></tr>
                <tr><td style="padding:4px 16px 4px 0;color:#666">Termin</td><td><strong>{when}</strong></td></tr>
              </table>
              <p>Den Link zum Absagen finden Sie in Ihrer Bestätigungsmail.</p>
              {contactHtml}
              <p style="color:#666;font-size:13px">Viele Grüße<br>{business}</p>
            </div>
            """;

        var text = $"""
            Hallo {firstName}, wir möchten Sie an Ihren Termin erinnern:

            Behandlung: {treatmentName}
            Termin: {when}

            Den Link zum Absagen finden Sie in Ihrer Bestätigungsmail.
            {contactText}Viele Grüße
            {businessName}
            """;

        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) Cancellation(
        string firstName, string treatmentName, DateTime startUtc, string businessName, string bookUrl)
    {
        var when = FormatDateTime(startUtc);
        var name = WebUtility.HtmlEncode(firstName);
        var treatment = WebUtility.HtmlEncode(treatmentName);
        var business = WebUtility.HtmlEncode(businessName);
        var link = WebUtility.HtmlEncode(bookUrl);

        var subject = $"Ihr Termin wurde abgesagt – {businessName}";

        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#222;line-height:1.5">
              <h2 style="margin-bottom:4px">Ihr Termin wurde abgesagt</h2>
              <p>Hallo {name}, wir bestätigen die Absage:</p>
              <p><strong>{treatment}</strong><br>{when}</p>
              <p>Einen neuen Termin können Sie jederzeit hier buchen:<br><a href="{link}">Neuen Termin buchen</a></p>
              <p style="color:#666;font-size:13px">Viele Grüße<br>{business}</p>
            </div>
            """;

        var text = $"""
            Hallo {firstName}, wir bestätigen die Absage:

            {treatmentName}
            {when}

            Einen neuen Termin können Sie jederzeit hier buchen: {bookUrl}

            Viele Grüße
            {businessName}
            """;

        return (subject, html, text);
    }
}