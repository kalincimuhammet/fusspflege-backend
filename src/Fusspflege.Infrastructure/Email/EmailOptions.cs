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
}