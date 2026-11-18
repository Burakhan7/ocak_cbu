using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UOM.Application.Abstractions;
using UOM.Infrastructure.Features.Admin;
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
            return services;
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(3);
            }));

        services.AddDataProtection();

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<NewsPostAdminService>();
        services.AddScoped<EventAdminService>();
        services.AddScoped<VenueAdminService>();
        services.AddScoped<SiteSettingsAdminService>();
        services.AddScoped<MediaAdminService>();
        services.AddScoped<DashboardAdminService>();

        return services;
    }
}