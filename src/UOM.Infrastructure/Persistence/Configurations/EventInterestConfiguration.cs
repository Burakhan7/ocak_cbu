using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class EventInterestConfiguration : IEntityTypeConfiguration<EventInterest>
{
    public void Configure(EntityTypeBuilder<EventInterest> builder)
    {
        builder.ToTable("EventInterests");
        builder.ConfigureAuditFields();

        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.HasOne(e => e.Event).WithMany(ev => ev.Interests).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Faculty).WithMany().HasForeignKey(e => e.FacultyId).OnDelete(DeleteBehavior.SetNull);

    }
}
