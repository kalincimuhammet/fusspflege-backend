using Fusspflege.Core.Enums;

namespace Fusspflege.Core.Entities;

public class AppointmentLog
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public AppointmentStatus? OldStatus { get; set; }
    public AppointmentStatus NewStatus { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public ChangeSource Source { get; set; }
}