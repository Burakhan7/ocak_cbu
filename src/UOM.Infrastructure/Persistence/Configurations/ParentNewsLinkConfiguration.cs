using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class ParentNewsLinkConfiguration : IEntityTypeConfiguration<ParentNewsLink>
{
    public void Configure(EntityTypeBuilder<ParentNewsLink> builder)
    {
        builder.ToTable("ParentNewsLinks");
        builder.ConfigureAuditFields();

        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.ExternalUrl).HasMaxLength(2000).IsRequired();
        builder.HasOne(e => e.Image).WithMany().HasForeignKey(e => e.ImageId).OnDelete(DeleteBehavior.Restrict);

    }
}
