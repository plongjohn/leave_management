using LeaveManagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.Infrastructure;

/// <summary>Resets the database before each test and offers a helper to arrange data.</summary>
public abstract class ApiTestBase(ApiTestFixture fixture) : IAsyncLifetime
{
    protected HttpClient Client { get; } = fixture.Factory.CreateClient();

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync()
    {
        await fixture.Database.ResetAsync();
    }

    public virtual ValueTask DisposeAsync()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected async Task ArrangeAsync(Action<LeaveManagementDbContext> arrange)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        arrange(dbContext);
        await dbContext.SaveChangesAsync(CancellationToken);
    }

    /// <summary>A new scope with its own DbContext, e.g. to simulate a second concurrent request.</summary>
    protected AsyncServiceScope CreateScope() => fixture.Factory.Services.CreateAsyncScope();
}
