namespace Fusspflege.Core.Entities;

public class Treatment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int PriceCents { get; set; }
    public bool IsActive { get; set; } = true;
}