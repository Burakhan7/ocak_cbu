using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UOM.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IdentityBuilder AddUomAdminIdentity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        return services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<UOM.Infrastructure.Persistence.AppDbContext>()
            .AddDefaultTokenProviders();
    }

    public static void ConfigureUomAdminCookies(this IServiceCollection services, IHostEnvironment environment)
    {
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/yonetim/hesap/giris";
            options.LogoutPath = "/yonetim/hesap/cikis";
            options.AccessDeniedPath = "/yonetim/hesap/erisim-reddedildi";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsEnvironment("Docker") && !environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });
    }
}