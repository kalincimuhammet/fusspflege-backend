using System.Globalization;

namespace Fusspflege.Core.Services;

// Erzeugt eine Kalenderdatei (iCalendar, RFC 5545), die Outlook, Apple Kalender und Google Kalender öffnen
public static class IcsBuilder
{
    public static string Build(
        string uid,
        string summary,
        string description,
        string location,
        DateTime startUtc,
        DateTime endUtc,
        DateTime nowUtc)
    {
        static string Fmt(DateTime d) =>
            d.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        static string Esc(string s) => s
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");

        var lines = new[]
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Fusspflege//Terminbuchung//DE",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{uid}",
            $"DTSTAMP:{Fmt(nowUtc)}",
            $"DTSTART:{Fmt(startUtc)}",
            $"DTEND:{Fmt(endUtc)}",
            $"SUMMARY:{Esc(summary)}",
            $"DESCRIPTION:{Esc(description)}",
            $"LOCATION:{Esc(location)}",
            "END:VEVENT",
            "END:VCALENDAR"
        };

        // Zeilenenden müssen laut Standard CRLF sein
        return string.Join("\r\n", lines) + "\r\n";
    }
}