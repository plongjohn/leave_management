using System.Security.Cryptography;
using LeaveManagement.Api.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeaveManagement.Api.Tests.Infrastructure;

public sealed class LeaveManagementApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string Issuer = "leave-management-api";
    public const string Audience = "leave-management-app";

    /// <summary>"Now" for the business rules: Monday, 2 March 2026, 09:00 in Vienna.</summary>
    public static readonly DateTimeOffset DefaultCompanyNow = new(2026, 3, 2, 8, 0, 0, TimeSpan.Zero);

    // High enough that the tests never hit it; the limit itself is tested with its own host.
    private const int LoginPermitLimit = 10_000;

    /// <summary>A new random key per test run, base64-encoded like the real configuration.</summary>
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// The time behind <see cref="CompanyClock"/>, so business rules see a fixed "today". Tokens and
    /// cookies keep the real time: their validation does not go through this clock.
    /// </summary>
    public TestCompanyTime CompanyTime { get; } = new(DefaultCompanyNow);

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

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<CompanyClock>();
            services.AddSingleton(new CompanyClock(CompanyTime));
        });
    }
}
