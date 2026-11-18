using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Infrastructure.Features.Admin;
using UOM.Infrastructure.Features.Public;
using UOM.Infrastructure.Html;
using UOM.Infrastructure.Identity;
using UOM.Infrastructure.Imaging;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Seed;
using UOM.Infrastructure.Security;
using UOM.Infrastructure.Slugs;
using UOM.Infrastructure.Storage;
using UOM.Infrastructure.Time;

namespace UOM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISlugService, SlugService>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<IImageProcessor, ImageSharpImageProcessor>();
        services.AddSingleton<IPreviewTokenService, PreviewTokenService>();
        services.AddSingleton<IHtmlSanitizer, GanssHtmlSanitizer>();
        services.AddSingleton<IIpHasher, IpHasher>();
        services.AddHttpClient(nameof(TurnstileVerifier));
        services.AddSingleton<ITurnstileVerifier, TurnstileVerifier>();
        services.AddMemoryCache();

        services.AddHttpContextAccessor();
        services.AddScoped<IAuditUserProvider>(sp =>
        {
            var accessor = sp.GetService<IHttpContextAccessor>();
            return accessor is null
                ? new NullAuditUserProvider()
                : new HttpContextAuditUserProvider(accessor);
        });

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ISiteSettingsProvider, EmptySiteSettingsProvider>();
            services.AddSingleton<IPublicNewsService, EmptyPublicNewsService>();
            services.AddSingleton<IPublicEventService, EmptyPublicEventService>();
            services.AddSingleton<IPublicPageService, EmptyPublicPageService>();
            services.AddSingleton<IContactFormService, EmptyContactFormService>();
            services.AddSingleton<IPublicCampusHubService, EmptyPublicCampusHubService>();
            services.AddSingleton<IPublicGuideService, EmptyPublicGuideService>();
            services.AddSingleton<IPublicResourceService, EmptyPublicResourceService>();
            services.AddSingleton<IPublicAnnouncementService, EmptyPublicAnnouncementService>();
            return services;
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            if (configuration.GetValue("Database:UseInMemory", false))
            {
                var name = configuration["Database:InMemoryName"] ?? "UOM_Test";
                options.UseInMemoryDatabase(name);
                return;
            }

            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(3);
            });
        });

        services.AddDataProtection();

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<NewsPostAdminService>();
        services.AddScoped<EventAdminService>();
        services.AddScoped<VenueAdminService>();
        services.AddScoped<SiteSettingsAdminService>();
        services.AddScoped<MediaAdminService>();
        services.AddScoped<DashboardAdminService>();
        services.AddScoped<CampusAdminService>();
        services.AddScoped<GuideArticleAdminService>();
        services.AddScoped<ResourceAdminService>();
        services.AddScoped<AnnouncementAdminService>();
        services.AddScoped<IPublicNewsService, PublicNewsService>();
        services.AddScoped<IPublicEventService, PublicEventService>();
        services.AddScoped<IPublicPageService, PublicPageService>();
        services.AddScoped<IContactFormService, ContactFormService>();
        services.AddScoped<IPublicCampusHubService, PublicCampusHubService>();
        services.AddScoped<IPublicGuideService, PublicGuideService>();
        services.AddScoped<IPublicResourceService, PublicResourceService>();
        services.AddScoped<IPublicAnnouncementService, PublicAnnouncementService>();
        services.AddScoped<ISiteSettingsProvider, SiteSettingsProvider>();

        return services;
    }
}