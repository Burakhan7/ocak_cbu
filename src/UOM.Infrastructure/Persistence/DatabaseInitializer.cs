using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UOM.Infrastructure.Persistence.Seed;

namespace UOM.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private const int DefaultMaxAttempts = 12;

    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await MigrateWithRetryAsync(db, logger, cancellationToken);
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration or seed failed.");
            throw;
        }
    }

    internal static async Task MigrateWithRetryAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken,
        int maxAttempts = DefaultMaxAttempts)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                if (attempt > 1)
                {
                    logger.LogInformation("Database migration succeeded on attempt {Attempt}.", attempt);
                }

                return;
            }
            catch (SqlException ex) when (attempt < maxAttempts && ShouldRetryMigration(ex.Number))
            {
                var delay = BackoffDelay(attempt);
                logger.LogWarning(
                    ex,
                    "Database migration attempt {Attempt}/{MaxAttempts} failed (SQL {SqlError}). Retrying in {DelaySeconds:0}s.",
                    attempt,
                    maxAttempts,
                    ex.Number,
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    internal static bool ShouldRetryMigration(int sqlErrorNumber) =>
        sqlErrorNumber == 1801 // database already exists (race on SqlServerDatabaseCreator)
        || IsTransientSql(sqlErrorNumber);

    internal static bool IsTransientSql(int sqlErrorNumber) =>
        sqlErrorNumber is 4060 or 40613 or 40197 or 40501 or 49918 or 49919 or 49920 or 4221 or 615 or 20 or 64 or 233
            or 10053 or 10054 or 10060 or 11001
            or -2 or -1;

    private static TimeSpan BackoffDelay(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, Math.Min(attempt, 5))));
}