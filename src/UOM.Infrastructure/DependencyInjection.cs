using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UOM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO(spec): Faz 1 — EF Core, persistence, migrations
        _ = configuration;
        return services;
    }
}