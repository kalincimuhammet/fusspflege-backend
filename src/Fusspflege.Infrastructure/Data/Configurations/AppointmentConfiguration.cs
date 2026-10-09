using Fusspflege.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fusspflege.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        // SHA-256 als Hex-String = 64 Zeichen
        builder.Property(a => a.CancelTokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(a => a.CancelTokenHash).IsUnique();

        // Doppelbuchungsschutz: pro Startzeit darf nur ein aktiver Termin existieren.
        // Status 4 = Cancelled, stornierte Termine blockieren den Slot nicht.
        builder.HasIndex(a => a.StartUtc)
            .IsUnique()
            .HasFilter("[Status] <> 4");

        // Beschleunigt die Abfrage des Erinnerungsjobs
        builder.HasIndex(a => new { a.Status, a.StartUtc });

        builder.HasOne(a => a.Customer)
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Treatment)
            .WithMany()
            .HasForeignKey(a => a.TreatmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Appointment_Range", "[EndUtc] > [StartUtc]");
            t.HasCheckConstraint("CK_Appointment_Status", "[Status] IN (1, 2, 3, 4)");
        });
    }
}