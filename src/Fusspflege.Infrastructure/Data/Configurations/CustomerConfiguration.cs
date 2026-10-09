using Fusspflege.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fusspflege.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(254);
        builder.Property(c => c.Street).IsRequired().HasMaxLength(200);
        builder.Property(c => c.PostalCode).IsRequired().HasMaxLength(10);
        builder.Property(c => c.City).IsRequired().HasMaxLength(100);

        // Ein Kunde pro E-Mail-Adresse, wiederkehrende Kunden werden wiederverwendet
        builder.HasIndex(c => c.Email).IsUnique();
    }
}