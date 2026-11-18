using System.Globalization;
using Serilog;
using UOM.Application;
using UOM.Infrastructure;
using UOM.Infrastructure.Persistence;

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
    builder.Services.AddControllersWithViews();

    var healthChecks = builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

    var dbConnection = builder.Configuration.GetConnectionString("Default");
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
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    if (!app.Environment.IsEnvironment("Docker"))
    {
        app.UseHttpsRedirection();
    }

    app.UseRouting();
    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapHealthChecks("/health");

    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

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