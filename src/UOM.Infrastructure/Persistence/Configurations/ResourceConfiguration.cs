using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");
        builder.ConfigureAuditFields();
        builder.ConfigureSlugIndex();
        builder.ConfigurePublishableIndexes();

        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(120).IsRequired();
        builder.HasOne(e => e.File).WithMany().HasForeignKey(e => e.FileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Faculty).WithMany(f => f.Resources).HasForeignKey(e => e.FacultyId).OnDelete(DeleteBehavior.SetNull);

    }
}
