using System.Net;
using LeaveManagement.Api.Tests.Infrastructure;

namespace LeaveManagement.Api.Tests;

public sealed class HealthTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Health_DatabaseReachable_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(CancellationToken));
    }
}
