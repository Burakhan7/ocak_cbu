using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("SiteSettings");
        builder.ConfigureAuditFields();

        builder.Property(e => e.SiteName).HasMaxLength(200);
        builder.Property(e => e.SocialLinks).HasColumnType("nvarchar(max)");
        builder.Property(e => e.GenelMerkezSocialLinks).HasColumnType("nvarchar(max)");
        builder.Property(e => e.HomeSectionsJson).HasColumnType("nvarchar(max)");
        builder.HasOne(e => e.LogoImage).WithMany().HasForeignKey(e => e.LogoImageId).OnDelete(DeleteBehavior.Restrict);
    }
}
