using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using UOM.Application;
using UOM.Infrastructure;
using UOM.Infrastructure.Identity;
using UOM.Infrastructure.Persistence;
using UOM.Web.Authorization;
using UOM.Web.Filters;

var turkishCulture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkishCulture;
CultureInfo.DefaultThreadCurrentUICulture = turkishCulture;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var dbConnection = builder.Configuration.GetConnectionString("Default");
    if (!string.IsNullOrWhiteSpace(dbConnection))
    {
        builder.Services.AddUomAdminIdentity();
        builder.Services.ConfigureUomAdminCookies(builder.Environment);
    }

    builder.Services.AddUomAuthorizationPolicies();
    builder.Services.AddScoped<AdminSecurityFilter>();

    var loginPerMinute = builder.Configuration.GetValue("RateLimit:LoginPerMinutePerIp", 5);
    var formsPerMinute = builder.Configuration.GetValue("RateLimit:FormsPerMinutePerIp", 3);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        var partitionKey = (HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = loginPerMinute,
                    QueueLimit = 0,
                }));
        options.AddPolicy("forms", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = formsPerMinute,
                    QueueLimit = 0,
                }));
    });

    builder.Services.AddControllersWithViews();

    var healthChecks = builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

    if (!string.IsNullOrWhiteSpace(dbConnection))
    {
        healthChecks.AddSqlServer(
            dbConnection,
            name: "sqlserver",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: ["db", "sql"]);
    }

    var app = builder.Build();

    if (app.Configuration.GetValue("Database:AutoMigrate", false)
        && !string.IsNullOrWhiteSpace(dbConnection))
    {
        await DatabaseInitializer.MigrateAndSeedAsync(app.Services);
    }

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/hata");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseStatusCodePagesWithReExecute("/bulunamadi");

    if (!app.Environment.IsEnvironment("Docker"))
    {
        app.UseHttpsRedirection();
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapHealthChecks("/health");
    app.MapControllers();

    app.MapAreaControllerRoute(
        name: "yonetim",
        areaName: "Admin",
        pattern: "yonetim/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "static-page",
        pattern: "{slug}",
        defaults: new { controller = "Page", action = "Detail" });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;