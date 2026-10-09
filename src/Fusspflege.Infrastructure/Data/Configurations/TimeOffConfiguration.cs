using Fusspflege.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fusspflege.Infrastructure.Data.Configurations;

public class TimeOffConfiguration : IEntityTypeConfiguration<TimeOff>
{
    public void Configure(EntityTypeBuilder<TimeOff> builder)
    {
        builder.Property(t => t.Reason).HasMaxLength(200);

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_TimeOff_Range", "[StartDate] <= [EndDate]"));
    }
}