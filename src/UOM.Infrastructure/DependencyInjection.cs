using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UOM.Application.Abstractions;
using UOM.Application.Features.Public;
using UOM.Infrastructure.Caching;
using UOM.Infrastructure.Features.Admin;
using UOM.Infrastructure.Features.Public;
using UOM.Infrastructure.Html;
using UOM.Infrastructure.Identity;
using UOM.Infrastructure.Imaging;
using UOM.Infrastructure.Persistence;
using UOM.Infrastructure.Persistence.Seed;
using UOM.Infrastructure.Privacy;
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
        services.AddSingleton<ICacheInvalidator, NullCacheInvalidator>();
        services.AddSingleton<ContentCacheInvalidationInterceptor>();
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
            services.AddSingleton<IPublicQuoteService, EmptyPublicQuoteService>();
            services.AddSingleton<IPublicParentHubService, EmptyPublicParentHubService>();
            services.AddSingleton<IPublicOrganizationService, EmptyPublicOrganizationService>();
            services.AddSingleton<IPublicVideoService, EmptyPublicVideoService>();
            services.AddSingleton<IPublicSearchService, EmptyPublicSearchService>();
            services.AddSingleton<ISitemapService, EmptySitemapService>();
            services.AddSingleton<IFeedService, EmptyFeedService>();
            services.AddSingleton<IMediaAssetReader, EmptyMediaAssetReader>();
            services.AddSingleton<IPublicRedirectService, EmptyPublicRedirectService>();
            services.AddSingleton<IHomeFeedService, EmptyHomeFeedService>();
            return services;
        }

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<ContentCacheInvalidationInterceptor>());
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
        services.AddScoped<QuoteAdminService>();
        services.AddScoped<ParentNewsLinkAdminService>();
        services.AddScoped<ParentProjectAdminService>();
        services.AddScoped<MagazineIssueAdminService>();
        services.AddScoped<VideoAdminService>();
        services.AddScoped<DistrictAdminService>();
        services.AddScoped<BoardMemberAdminService>();
        services.AddScoped<IPublicQuoteService, PublicQuoteService>();
        services.Configure<ParentSiteOptions>(configuration.GetSection(ParentSiteOptions.SectionName));
        services.AddHttpClient(ParentSiteFeed.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(8);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("UOM-Manisa/1.0");
                client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/atom+xml");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/xml");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 3,
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            });
        services.AddSingleton<IParentSiteFeed, ParentSiteFeed>();
        services.AddScoped<IHomeFeedService, HomeFeedService>();
        services.AddScoped<IPublicParentHubService, PublicParentHubService>();
        services.AddScoped<IPublicOrganizationService, PublicOrganizationService>();
        services.AddScoped<IPublicVideoService, PublicVideoService>();
        services.AddScoped<IPublicNewsService, PublicNewsService>();
        services.AddScoped<IPublicEventService, PublicEventService>();
        services.AddScoped<IPublicPageService, PublicPageService>();
        services.AddScoped<IContactFormService, ContactFormService>();
        services.AddScoped<IPublicCampusHubService, PublicCampusHubService>();
        services.AddScoped<IPublicGuideService, PublicGuideService>();
        services.AddScoped<IPublicResourceService, PublicResourceService>();
        services.AddScoped<IPublicAnnouncementService, PublicAnnouncementService>();
        services.AddScoped<ISiteSettingsProvider, SiteSettingsProvider>();
        services.AddScoped<RedirectService>();
        services.AddScoped<IRedirectRecorder>(sp => sp.GetRequiredService<RedirectService>());
        services.AddScoped<IPublicRedirectService>(sp => sp.GetRequiredService<RedirectService>());
        services.AddScoped<IPublicSearchService, PublicSearchService>();
        services.AddScoped<ISitemapService, SitemapService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IMediaAssetReader, MediaAssetReader>();
        services.AddScoped<RedirectAdminService>();
        services.AddScoped<KvkkRetentionService>();
        services.AddScoped<DataSubjectRequestService>();
        services.AddHostedService<KvkkRetentionHostedService>();

        return services;
    }
}