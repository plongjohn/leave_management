using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.Auth;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LeaveManagement.Api.Tests.Auth;

public sealed class LoginTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    private const int AccessTokenLifetimeSeconds = 15 * 60;
    private const int MaxFailedLoginAttempts = 5;

    private readonly Employee _employee =
        Employee.Create("Sabine", "Huber", "sabine.huber@example.at", new DateOnly(2015, 3, 1), EmployeeRole.Hr);

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessTokenWithEmployeeAndRole()
    {
        var user = await CreateUserAsync(_employee);

        var response = await LoginAsync(_employee.Email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("Bearer", body.TokenType);
        Assert.InRange(body.ExpiresIn, AccessTokenLifetimeSeconds - 5, AccessTokenLifetimeSeconds);

        var token = new JsonWebToken(body.AccessToken);
        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(_employee.Id.ToString(), token.GetClaim("employee_id").Value);
        Assert.Equal("Hr", token.GetClaim("role").Value);
        Assert.Equal(LeaveManagementApiFactory.Issuer, token.Issuer);
        Assert.Contains(LeaveManagementApiFactory.Audience, token.Audiences);
        Assert.False(string.IsNullOrEmpty(token.Id));
    }

    [Fact]
    public async Task Login_EmailInOtherCase_ReturnsAccessToken()
    {
        await CreateUserAsync(_employee);

        var response = await LoginAsync("Sabine.Huber@Example.at");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedWithCode()
    {
        await CreateUserAsync(_employee);

        var response = await LoginAsync(_employee.Email, "wrong password, but long");

        await AssertLoginFailedAsync(response);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsSameResponseAsWrongPassword()
    {
        await CreateUserAsync(_employee);

        var response = await LoginAsync("nobody@example.at");

        await AssertLoginFailedAsync(response);
    }

    [Fact]
    public async Task Login_AfterMaxFailedAttempts_RejectsCorrectPassword()
    {
        await CreateUserAsync(_employee);
        for (var attempt = 0; attempt < MaxFailedLoginAttempts; attempt++)
        {
            await LoginAsync(_employee.Email, "wrong password, but long");
        }

        var response = await LoginAsync(_employee.Email);

        await AssertLoginFailedAsync(response);
    }

    [Fact]
    public async Task Login_SuccessAfterFailedAttempts_ResetsFailureCount()
    {
        await CreateUserAsync(_employee);
        for (var attempt = 0; attempt < MaxFailedLoginAttempts - 1; attempt++)
        {
            await LoginAsync(_employee.Email, "wrong password, but long");
        }

        await LoginAsync(_employee.Email);
        await LoginAsync(_employee.Email, "wrong password, but long");
        var response = await LoginAsync(_employee.Email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyFields_ReturnsValidationProblem()
    {
        var response = await LoginAsync(string.Empty, string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Email", problem.Errors.Keys);
        Assert.Contains("Password", problem.Errors.Keys);
    }

    [Fact]
    public async Task Login_MoreAttemptsThanPermitted_ReturnsTooManyRequests()
    {
        const int permitLimit = 2;
        await using var limitedFactory = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RateLimiting:Login:PermitLimit", permitLimit.ToString(CultureInfo.InvariantCulture)));
        using var client = limitedFactory.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var request = new LoginRequest("nobody@example.at", "wrong password, but long");

        for (var attempt = 0; attempt < permitLimit; attempt++)
        {
            await client.PostAsJsonAsync("/api/auth/login", request, CancellationToken);
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", request, CancellationToken);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    private static async Task AssertLoginFailedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(AuthErrors.LoginFailed, problem.Extensions["code"]?.ToString());
    }
}
