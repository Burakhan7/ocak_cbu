using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Entities;

namespace UOM.Infrastructure.Persistence.Configurations;

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        builder.ToTable("BoardMembers");
        builder.ConfigureAuditFields();

        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.HasOne(e => e.PhotoImage).WithMany().HasForeignKey(e => e.PhotoImageId).OnDelete(DeleteBehavior.Restrict);

    }
}
