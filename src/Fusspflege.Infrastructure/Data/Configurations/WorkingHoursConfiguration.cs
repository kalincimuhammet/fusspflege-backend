using Fusspflege.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fusspflege.Infrastructure.Data.Configurations;

public class WorkingHoursConfiguration : IEntityTypeConfiguration<WorkingHours>
{
    public void Configure(EntityTypeBuilder<WorkingHours> builder)
    {
        builder.HasIndex(w => w.Weekday);

        builder.ToTable(t =>
            t.HasCheckConstraint("CK_WorkingHours_Range", "[StartTime] < [EndTime]"));
    }
}