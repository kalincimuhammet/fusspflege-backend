namespace Fusspflege.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    // "Console" (Entwicklung, Mail erscheint im Terminal) oder "Acs" (echter Versand)
    public string Provider { get; set; } = "Console";

    public string? AcsConnectionString { get; set; }
    public string SenderAddress { get; set; } = string.Empty;

    // Adresse des Angular-Frontends, daraus wird der Stornolink gebaut
    public string PublicBaseUrl { get; set; } = "http://localhost:4200";

    public string BusinessName { get; set; } = "Mobile Fußpflege";
    // Kontaktdaten, die in der Erinnerungsmail stehen (z. B. Telefonnummer), leer = kein Hinweis
    public string ContactInfo { get; set; } = string.Empty;

    // Empfänger der Benachrichtigungen an die Praxis, leer = keine Benachrichtigung
    public string AdminNotificationAddress { get; set; } = string.Empty;
}