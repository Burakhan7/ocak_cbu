using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class MagazineIssueConfiguration : IEntityTypeConfiguration<MagazineIssue>
{
    public void Configure(EntityTypeBuilder<MagazineIssue> builder)
    {
        builder.ToTable("MagazineIssues");
        builder.ConfigureAuditFields();

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExternalUrl).HasMaxLength(2000).IsRequired();
        builder.HasOne(e => e.CoverImage).WithMany().HasForeignKey(e => e.CoverImageId).OnDelete(DeleteBehavior.Restrict);

    }
}
