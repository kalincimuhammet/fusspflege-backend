using Fusspflege.Core.Enums;

namespace Fusspflege.Core.Entities;

public class Appointment
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int TreatmentId { get; set; }
    public Treatment Treatment { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;

    // Nur der Hash wird gespeichert, der Klartext-Token steht nur im Link der Mail
    public string CancelTokenHash { get; set; } = string.Empty;

    public DateTime? ReminderSentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public List<AppointmentLog> Logs { get; set; } = new();
}