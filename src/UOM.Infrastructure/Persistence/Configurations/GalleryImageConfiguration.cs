using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class GalleryImageConfiguration : IEntityTypeConfiguration<GalleryImage>
{
    public void Configure(EntityTypeBuilder<GalleryImage> builder)
    {
        builder.ToTable("GalleryImages");
        builder.ConfigureAuditFields();

        builder.HasOne(e => e.Album).WithMany(a => a.Images).HasForeignKey(e => e.AlbumId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.MediaFile).WithMany().HasForeignKey(e => e.MediaFileId).OnDelete(DeleteBehavior.Restrict);

    }
}
