using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class RedirectConfiguration : IEntityTypeConfiguration<Redirect>
{
    public void Configure(EntityTypeBuilder<Redirect> builder)
    {
        builder.ToTable("Redirects");
        builder.ConfigureAuditFields();

        builder.Property(e => e.FromPath).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ToPath).HasMaxLength(500).IsRequired();
        builder.HasIndex(e => e.FromPath).IsUnique().HasFilter("[IsDeleted] = 0");

    }
}
