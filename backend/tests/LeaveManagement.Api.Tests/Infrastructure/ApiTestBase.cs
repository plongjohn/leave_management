using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeaveManagement.Api.Auth;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Infrastructure.Identity;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.Infrastructure;

/// <summary>Resets the database before each test and offers helpers to arrange data and sign in.</summary>
public abstract class ApiTestBase(ApiTestFixture fixture) : IAsyncLifetime
{
    /// <summary>Satisfies the password policy (at least 12 characters).</summary>
    protected const string Password = "correct horse battery staple";

    protected HttpClient Client { get; } = fixture.Factory.CreateHttpsClient();

    protected LeaveManagementApiFactory Factory => fixture.Factory;

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

    /// <summary>Saves the employee together with a login, created through Identity like in production.</summary>
    protected async Task<ApplicationUser> CreateUserAsync(Employee employee, string password = Password)
    {
        await ArrangeAsync(dbContext => dbContext.Employees.Add(employee));

        await using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = ApplicationUser.ForEmployee(employee);
        var result = await userManager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));

        return user;
    }

    protected async Task<HttpResponseMessage> LoginAsync(string email, string password = Password)
    {
        return await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), CancellationToken);
    }

    /// <summary>Creates a login for the employee, signs in and sends the access token with every request.</summary>
    protected async Task SignInAsync(Employee employee)
    {
        await CreateUserAsync(employee);
        var response = await LoginAsync(employee.Email);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(CancellationToken);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
    }
}
