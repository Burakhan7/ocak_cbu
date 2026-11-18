using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class GalleryAlbumConfiguration : IEntityTypeConfiguration<GalleryAlbum>
{
    public void Configure(EntityTypeBuilder<GalleryAlbum> builder)
    {
        builder.ToTable("GalleryAlbums");
        builder.ConfigureAuditFields();
        builder.ConfigureSlugIndex();
        builder.ConfigurePublishableIndexes();

        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(120).IsRequired();
        builder.HasOne(e => e.CoverImage).WithMany().HasForeignKey(e => e.CoverImageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Event).WithMany(ev => ev.GalleryAlbums).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.SetNull);

    }
}
