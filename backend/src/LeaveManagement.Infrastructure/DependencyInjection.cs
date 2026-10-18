using LeaveManagement.Infrastructure.Persistence;
using LeaveManagement.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        bool seedDevelopmentData)
    {
        services.AddDbContext<LeaveManagementDbContext>(options =>
        {
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();

            // EF Core runs these hooks after migrating; the tooling (dotnet ef) uses the
            // synchronous one, the application the asynchronous one.
            if (seedDevelopmentData)
            {
                options
                    .UseSeeding((context, _) => DevelopmentDataSeeder.Seed(context))
                    .UseAsyncSeeding((context, _, cancellationToken) =>
                        DevelopmentDataSeeder.SeedAsync(context, cancellationToken));
            }
        });

        return services;
    }
}
