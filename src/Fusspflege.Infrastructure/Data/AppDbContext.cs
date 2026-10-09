using Fusspflege.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fusspflege.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Treatment> Treatments => Set<Treatment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<TimeOff> TimeOffs => Set<TimeOff>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentLog> AppointmentLogs => Set<AppointmentLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Findet automatisch alle Konfigurationsklassen im Ordner Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}