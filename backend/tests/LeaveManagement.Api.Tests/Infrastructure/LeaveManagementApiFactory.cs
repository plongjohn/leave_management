using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LeaveManagement.Api.Tests.Infrastructure;

public sealed class LeaveManagementApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string Issuer = "leave-management-api";
    public const string Audience = "leave-management-app";

    // High enough that the tests never hit it; the limit itself is tested with its own host.
    private const int LoginPermitLimit = 10_000;

    /// <summary>A new random key per test run, base64-encoded like the real configuration.</summary>
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>Uses HTTPS, otherwise the client would not send the secure refresh cookie back.</summary>
    public HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development": no demo data, no OpenAPI UI.
        builder.UseEnvironment("Testing");

        // Takes precedence over the developer's ConnectionStrings__LeaveManagement, so the
        // tests never touch the development database (checked by ApiTestFixture).
        builder.UseSetting("ConnectionStrings:LeaveManagement", connectionString);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("RateLimiting:Login:PermitLimit", LoginPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
