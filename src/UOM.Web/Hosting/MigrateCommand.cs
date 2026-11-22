using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using UOM.Infrastructure;
using UOM.Infrastructure.Identity;
using UOM.Infrastructure.Persistence;

namespace UOM.Web.Hosting;

public static class MigrateCommand
{
    public static bool IsRequested(string[] args) =>
        args.Any(a => string.Equals(a, "--migrate", StringComparison.OrdinalIgnoreCase));

    public static async Task<int> RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        var connectionString = builder.Configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString)
            || builder.Configuration.GetValue("Database:UseInMemory", false))
        {
            Log.Error("--migrate requires ConnectionStrings:Default pointing at SQL Server.");
            return 1;
        }

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddUomAdminIdentity();

        await using var app = builder.Build();
        try
        {
            await DatabaseInitializer.MigrateAndSeedAsync(app.Services);
            app.Logger.LogInformation("Database migration and seed finished. Exiting.");
            return 0;
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Database migration failed.");
            return 1;
        }
    }
}
