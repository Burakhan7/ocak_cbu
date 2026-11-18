using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UOM.Domain.Common;

namespace UOM.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static void ConfigureAuditFields<T>(this EntityTypeBuilder<T> builder)
        where T : BaseEntity
    {
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);
        builder.Property(e => e.UpdatedByUserId).HasMaxLength(450);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
    }

    public static void ConfigureSlugIndex<T>(this EntityTypeBuilder<T> builder, string slugPropertyName = "Slug")
        where T : BaseEntity
    {
        builder.HasIndex(slugPropertyName)
            .IsUnique()
            .HasFilter($"[{slugPropertyName}] IS NOT NULL AND [IsDeleted] = 0");
    }

    public static void ConfigurePublishableIndexes<T>(this EntityTypeBuilder<T> builder)
        where T : class, IPublishable
    {
        builder.HasIndex(e => new { e.Status, e.PublishedAtUtc });
    }
}