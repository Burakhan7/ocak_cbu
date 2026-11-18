using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class NewsPostConfiguration : IEntityTypeConfiguration<NewsPost>
{
    public void Configure(EntityTypeBuilder<NewsPost> builder)
    {
        builder.ToTable("NewsPosts");
        builder.ConfigureAuditFields();
        builder.ConfigureSlugIndex();
        builder.ConfigurePublishableIndexes();

        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Summary).HasMaxLength(500);
        builder.Property(e => e.LastRejectionNote).HasMaxLength(2000);
        builder.HasIndex(e => new { e.Status, e.PublishedAtUtc });
        builder.HasOne(e => e.CoverImage).WithMany().HasForeignKey(e => e.CoverImageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Category).WithMany(c => c.NewsPosts).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);

    }
}
