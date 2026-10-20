using LeaveManagement.Infrastructure.Persistence;
using LeaveManagement.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Infrastructure;

public static class DependencyInjection
{
    /// <param name="developmentSeed">Seeds demo data after migrating; <c>null</c> outside Development.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        DevelopmentSeedOptions? developmentSeed)
    {
        services.AddDbContext<LeaveManagementDbContext>(options =>
        {
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();

            // EF Core runs these hooks after migrating; the tooling (dotnet ef) uses the
            // synchronous one, the application the asynchronous one.
            if (developmentSeed is not null)
            {
                options
                    .UseSeeding((context, _) => DevelopmentDataSeeder.Seed(context, developmentSeed))
                    .UseAsyncSeeding((context, _, cancellationToken) =>
                        DevelopmentDataSeeder.SeedAsync(context, developmentSeed, cancellationToken));
            }
        });

        return services;
    }
}
