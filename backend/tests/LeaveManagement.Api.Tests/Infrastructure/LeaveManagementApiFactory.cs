using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LeaveManagement.Api.Tests.Infrastructure;

public sealed class LeaveManagementApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development": no demo data, no OpenAPI UI.
        builder.UseEnvironment("Testing");

        // Takes precedence over the developer's ConnectionStrings__LeaveManagement, so the
        // tests never touch the development database (checked by ApiTestFixture).
        builder.UseSetting("ConnectionStrings:LeaveManagement", connectionString);
    }
}
