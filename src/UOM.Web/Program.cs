using System.Globalization;
using System.IO.Compression;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using UOM.Application;
using UOM.Application.Abstractions;
using UOM.Infrastructure;
using UOM.Infrastructure.Identity;
using UOM.Infrastructure.Persistence;
using UOM.Web.Authorization;
using UOM.Web.Caching;
using UOM.Web.Filters;
using UOM.Web.Hosting;
using UOM.Web.Middleware;
using UOM.Web.Routing;
using UOM.Web.Security;

var turkishCulture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkishCulture;
CultureInfo.DefaultThreadCurrentUICulture = turkishCulture;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    if (MigrateCommand.IsRequested(args))
    {
        Environment.ExitCode = await MigrateCommand.RunAsync(args);
        return;
    }

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.Configure<UOM.Web.Configuration.GoogleMapsOptions>(
        builder.Configuration.GetSection(UOM.Web.Configuration.GoogleMapsOptions.SectionName));
    builder.Services.PostConfigure<UOM.Web.Configuration.GoogleMapsOptions>(maps =>
    {
        if (string.IsNullOrWhiteSpace(maps.ApiKey))
        {
            maps.ApiKey = builder.Configuration["GOOGLE_MAPS_API_KEY"];
        }
    });

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
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            context.HttpContext.Response.Headers.RetryAfter = "60";
            await context.HttpContext.Response.WriteAsync(
                "Çok fazla istek gönderildi. Lütfen bir dakika sonra yeniden deneyin.",
                cancellationToken);
        };
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

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            ["application/xml", "text/xml", "application/rss+xml", "image/svg+xml"]);
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });

    builder.Services.AddOutputCache();
    builder.Services.AddSingleton<ICacheInvalidator, OutputCacheInvalidator>();

    builder.Services.AddControllersWithViews();
    builder.Services.Configure<RouteOptions>(options =>
        options.ConstraintMap["nonreserved"] = typeof(NonReservedSlugRouteConstraint));

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

    app.UseResponseCompression();
    app.UseMiddleware<SecurityHeadersMiddleware>();
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
    app.UseMiddleware<TrailingSlashMiddleware>();

    if (!app.Environment.IsEnvironment("Docker"))
    {
        app.UseHttpsRedirection();
    }

    app.UseMiddleware<RobotsNoIndexMiddleware>();
    app.UseMiddleware<UploadGuardMiddleware>();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            var path = ctx.Context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Context.Response.Headers.CacheControl = "public, max-age=604800";
                return;
            }

            if (ctx.Context.Request.Query.ContainsKey("v"))
            {
                ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            }
        },
    });
    app.UseMiddleware<PublicRedirectMiddleware>();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<CspNonceMiddleware>();
    app.UseOutputCache();

    app.MapStaticAssets();
    app.MapHealthChecks("/health").AddEndpointFilter(async (context, next) =>
    {
        var env = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        if (!HealthCheckAccess.IsAllowed(context.HttpContext, env, config))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return await next(context);
    });

    app.MapAreaControllerRoute(
        name: "yonetim",
        areaName: "Admin",
        pattern: "yonetim/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllers();

    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapControllerRoute(
        name: "static-page",
        pattern: "{slug:nonreserved}",
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