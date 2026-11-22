using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UOM.Infrastructure.Privacy;

public sealed class KvkkRetentionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<KvkkRetentionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var zone = KvkkRetentionSchedule.IstanbulOrFixedOffset();
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = KvkkRetentionSchedule.DelayUntilNextRun(DateTime.UtcNow, zone);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var retention = scope.ServiceProvider.GetRequiredService<KvkkRetentionService>();
                await retention.PurgeExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "KVKK retention purge failed.");
            }
        }
    }
}
