using Fusspflege.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Infrastructure.Data;

public static class DbSeeder
{
    // Beispielwerte, Namen, Dauer und Preise passt dein Bekannter später im Admin-Bereich an
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Treatments.AnyAsync())
        {
            db.Treatments.AddRange(
                new Treatment { Name = "Basis-Fußpflege", DurationMinutes = 45, PriceCents = 3500 },
                new Treatment { Name = "Komplett-Fußpflege", DurationMinutes = 60, PriceCents = 4500 },
                new Treatment { Name = "Fußpflege mit Massage", DurationMinutes = 75, PriceCents = 5500 });
        }

        if (!await db.WorkingHours.AnyAsync())
        {
            var weekdays = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            };

            foreach (var day in weekdays)
            {
                db.WorkingHours.Add(new WorkingHours
                {
                    Weekday = day,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(12, 0)
                });
                db.WorkingHours.Add(new WorkingHours
                {
                    Weekday = day,
                    StartTime = new TimeOnly(13, 0),
                    EndTime = new TimeOnly(17, 0)
                });
            }
        }

        await db.SaveChangesAsync();
    }
}