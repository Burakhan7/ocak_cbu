using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class ParentProjectConfiguration : IEntityTypeConfiguration<ParentProject>
{
    public void Configure(EntityTypeBuilder<ParentProject> builder)
    {
        builder.ToTable("ParentProjects");
        builder.ConfigureAuditFields();

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExternalUrl).HasMaxLength(2000).IsRequired();
        builder.HasOne(e => e.LogoImage).WithMany().HasForeignKey(e => e.LogoImageId).OnDelete(DeleteBehavior.Restrict);

    }
}
