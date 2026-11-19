using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.ToTable("Faculties");
        builder.ConfigureAuditFields();

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.HasOne(e => e.Campus).WithMany(c => c.Faculties).HasForeignKey(e => e.CampusId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.CoverImage).WithMany().HasForeignKey(e => e.CoverImageId).OnDelete(DeleteBehavior.Restrict);
    }
}
