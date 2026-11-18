using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("Venues");
        builder.ConfigureAuditFields();

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.HasOne(e => e.Campus).WithMany(c => c.Venues).HasForeignKey(e => e.CampusId).OnDelete(DeleteBehavior.SetNull);

    }
}
