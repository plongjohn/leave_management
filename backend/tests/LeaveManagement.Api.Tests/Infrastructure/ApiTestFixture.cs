using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.Infrastructure;

/// <summary>Shared by all tests of the assembly: one test database and one API host.</summary>
public sealed class ApiTestFixture : IAsyncLifetime
{
    private TestDatabase? _database;
    private LeaveManagementApiFactory? _factory;

    public TestDatabase Database => _database ?? throw NotInitialized();

    public LeaveManagementApiFactory Factory => _factory ?? throw NotInitialized();

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _factory = new LeaveManagementApiFactory(_database.ConnectionString);
        EnsureApiUsesTestDatabase();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    // Guards against a configuration change that would let the tests reset the development database.
    private void EnsureApiUsesTestDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var databaseName = dbContext.Database.GetDbConnection().Database;

        if (databaseName != Database.Name)
        {
            throw new InvalidOperationException(
                $"The API under test uses the database '{databaseName}' instead of the test database '{Database.Name}'.");
        }
    }

    private static InvalidOperationException NotInitialized() => new("The test fixture is not initialized.");
}
