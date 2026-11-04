using System.Security.Claims;
using LeaveManagement.Api.Auth;
using LeaveManagement.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.Auth;

public sealed class AuthorizationTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    /// <summary>
    /// Deny by default: a new endpoint that allows anonymous access has to be added here on
    /// purpose. OpenAPI and Scalar are anonymous too, but only mapped in Development.
    /// </summary>
    [Fact]
    public void Endpoints_AllowingAnonymousAccess_AreOnlyTheExpectedOnes()
    {
        var anonymousEndpoints = Factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .Order(StringComparer.Ordinal);

        Assert.Equal(
            ["/api/auth/login", "/api/auth/logout", "/api/auth/refresh", "/health"],
            anonymousEndpoints);
    }

    [Theory]
    [InlineData(AuthorizationPolicies.ManagerOrHr, "Employee", false)]
    [InlineData(AuthorizationPolicies.ManagerOrHr, "Manager", true)]
    [InlineData(AuthorizationPolicies.ManagerOrHr, "Hr", true)]
    [InlineData(AuthorizationPolicies.Hr, "Employee", false)]
    [InlineData(AuthorizationPolicies.Hr, "Manager", false)]
    [InlineData(AuthorizationPolicies.Hr, "Hr", true)]
    public async Task Policy_Role_GrantsAccessAsExpected(string policy, string role, bool expected)
    {
        var authorization = Factory.Services.GetRequiredService<IAuthorizationService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", role)],
            authenticationType: "Test",
            nameType: "sub",
            roleType: "role"));

        var result = await authorization.AuthorizeAsync(user, policy);

        Assert.Equal(expected, result.Succeeded);
    }
}
