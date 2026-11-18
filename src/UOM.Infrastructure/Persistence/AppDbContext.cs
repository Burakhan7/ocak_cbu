using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UOM.Application.Abstractions;
using UOM.Domain.Common;
using UOM.Domain.Entities;
using UOM.Infrastructure.Identity;

namespace UOM.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IClock _clock;
    private readonly IAuditUserProvider _auditUserProvider;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IClock clock,
        IAuditUserProvider auditUserProvider)
        : base(options)
    {
        _clock = clock;
        _auditUserProvider = auditUserProvider;
    }

    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
    public DbSet<NewsPost> NewsPosts => Set<NewsPost>();
    public DbSet<ParentNewsLink> ParentNewsLinks => Set<ParentNewsLink>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<StaticPage> StaticPages => Set<StaticPage>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<GuideArticle> GuideArticles => Set<GuideArticle>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<ParentProject> ParentProjects => Set<ParentProject>();
    public DbSet<MagazineIssue> MagazineIssues => Set<MagazineIssue>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<GalleryAlbum> GalleryAlbums => Set<GalleryAlbum>();
    public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<EventInterest> EventInterests => Set<EventInterest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Redirect> Redirects => Set<Redirect>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _auditUserProvider.GetCurrentUserId();
        var utcNow = _clock.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.CreatedByUserId = userId;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedByUserId = userId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditLog>())
        {
            if (entry.State == EntityState.Added && entry.Entity.AtUtc == default)
            {
                entry.Entity.AtUtc = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(AppDbContext)
                .GetMethod(nameof(ConfigureSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [modelBuilder]);
        }
    }

    private static void ConfigureSoftDeleteFilter<T>(ModelBuilder modelBuilder)
        where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(entity => !entity.IsDeleted);
    }
}