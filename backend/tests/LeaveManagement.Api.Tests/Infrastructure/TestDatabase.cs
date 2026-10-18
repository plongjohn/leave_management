using System.Globalization;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace LeaveManagement.Api.Tests.Infrastructure;

/// <summary>
/// One PostgreSQL database per test run (ADR 0005): created and migrated at start,
/// reset with Respawn before each test, dropped at the end.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private const string NamePrefix = "leave_test_";
    private const string NameTimestampFormat = "yyyyMMddHHmmss";

    // Databases of aborted runs are removed by a later run once they are clearly abandoned.
    private static readonly TimeSpan AbandonedAfter = TimeSpan.FromDays(1);

    private readonly string _serverConnectionString;
    private Respawner? _respawner;

    private TestDatabase(string serverConnectionString, string name)
    {
        _serverConnectionString = serverConnectionString;
        Name = name;
        ConnectionString = new NpgsqlConnectionStringBuilder(serverConnectionString) { Database = name }.ConnectionString;
    }

    public string Name { get; }

    public string ConnectionString { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var serverConnectionString = ReadServerConnectionString();
        var timestamp = DateTime.UtcNow.ToString(NameTimestampFormat, CultureInfo.InvariantCulture);
        var database = new TestDatabase(serverConnectionString, $"{NamePrefix}{timestamp}_{Guid.NewGuid():N}");

        await database.DropAbandonedDatabasesAsync();
        await database.ExecuteOnServerAsync($"CREATE DATABASE \"{database.Name}\"");
        await database.MigrateAsync();

        return database;
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });

        await _respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)");
    }

    private static string ReadServerConnectionString()
    {
        return Environment.GetEnvironmentVariable("LEAVE_MANAGEMENT_TEST_DB")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__LeaveManagement")
            ?? throw new InvalidOperationException(
                "No database for the integration tests. Set ConnectionStrings__LeaveManagement " +
                "(see database/README.md) or LEAVE_MANAGEMENT_TEST_DB.");
    }

    private async Task MigrateAsync()
    {
        var options = new DbContextOptionsBuilder<LeaveManagementDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var dbContext = new LeaveManagementDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    private async Task DropAbandonedDatabasesAsync()
    {
        var abandonedBefore = DateTime.UtcNow - AbandonedAfter;

        await using var connection = new NpgsqlConnection(_serverConnectionString);
        await connection.OpenAsync();

        var names = new List<string>();
        await using (var command = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datname LIKE @prefix", connection))
        {
            command.Parameters.AddWithValue("prefix", NamePrefix + "%");
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }
        }

        foreach (var name in names.Where(name => WasCreatedBefore(name, abandonedBefore)))
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static bool WasCreatedBefore(string databaseName, DateTime threshold)
    {
        if (databaseName.Length < NamePrefix.Length + NameTimestampFormat.Length)
        {
            return false;
        }

        var timestamp = databaseName.Substring(NamePrefix.Length, NameTimestampFormat.Length);
        return DateTime.TryParseExact(
                   timestamp,
                   NameTimestampFormat,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                   out var createdAt)
               && createdAt < threshold;
    }

    private async Task ExecuteOnServerAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_serverConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
