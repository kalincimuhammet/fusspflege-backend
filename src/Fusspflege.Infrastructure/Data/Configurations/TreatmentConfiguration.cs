using Fusspflege.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fusspflege.Infrastructure.Data.Configurations;

public class TreatmentConfiguration : IEntityTypeConfiguration<Treatment>
{
    public void Configure(EntityTypeBuilder<Treatment> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Treatment_Duration", "[DurationMinutes] > 0");
            t.HasCheckConstraint("CK_Treatment_Price", "[PriceCents] >= 0");
        });
    }
}