namespace Fusspflege.Core.Entities;

public class WorkingHours
{
    public int Id { get; set; }
    public DayOfWeek Weekday { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}