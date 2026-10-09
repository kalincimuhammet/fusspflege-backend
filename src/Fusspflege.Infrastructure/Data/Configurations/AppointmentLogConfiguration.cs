using Fusspflege.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fusspflege.Infrastructure.Data.Configurations;

public class AppointmentLogConfiguration : IEntityTypeConfiguration<AppointmentLog>
{
    public void Configure(EntityTypeBuilder<AppointmentLog> builder)
    {
        builder.HasIndex(l => l.AppointmentId);

        builder.HasOne(l => l.Appointment)
            .WithMany(a => a.Logs)
            .HasForeignKey(l => l.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}