using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.Auth;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Infrastructure.Identity;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LeaveManagement.Api.Tests.Auth;

/// <summary>
/// Uses a client without its own cookie handling, so each test decides which refresh token it
/// sends – including old ones.
/// </summary>
public sealed class RefreshTokenTests : ApiTestBase
{
    private readonly HttpClient _client;

    private readonly Employee _employee =
        Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);

    public RefreshTokenTests(ApiTestFixture fixture)
        : base(fixture)
    {
        _client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false,
        });
    }

    public override ValueTask DisposeAsync()
    {
        _client.Dispose();
        return base.DisposeAsync();
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsRestrictedRefreshCookie()
    {
        await CreateUserAsync(_employee);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(_employee.Email, Password), CancellationToken);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("refresh_token=", cookie, StringComparison.Ordinal);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ValidCredentials_StoresOnlyTheHash()
    {
        await CreateUserAsync(_employee);

        var refreshToken = await LoginAndGetRefreshTokenAsync();

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var stored = await dbContext.RefreshTokens.SingleAsync(CancellationToken);
        Assert.Equal(RefreshTokenService.Hash(refreshToken), stored.TokenHash);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewAccessTokenAndRotatesCookie()
    {
        var user = await CreateUserAsync(_employee);
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        var response = await RefreshAsync(refreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(CancellationToken);
        var accessToken = new JsonWebToken(body!.AccessToken);
        Assert.Equal(user.Id.ToString(), accessToken.Subject);
        Assert.Equal(_employee.Id.ToString(), accessToken.GetClaim("employee_id").Value);
        Assert.Equal("Manager", accessToken.GetClaim("role").Value);

        var newRefreshToken = ReadRefreshToken(response);
        Assert.NotEqual(refreshToken, newRefreshToken);
    }

    [Fact]
    public async Task Refresh_RotatedToken_RevokesOldAndLinksNewOfSameFamily()
    {
        await CreateUserAsync(_employee);
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        var newRefreshToken = ReadRefreshToken(await RefreshAsync(refreshToken));

        var (old, current) = await LoadAsync(refreshToken, newRefreshToken);
        Assert.NotNull(old.RevokedAt);
        Assert.Equal(current.Id, old.ReplacedById);
        Assert.Equal(old.FamilyId, current.FamilyId);
        Assert.Null(current.RevokedAt);
    }

    [Fact]
    public async Task Refresh_NewTokenAfterRotation_Works()
    {
        await CreateUserAsync(_employee);
        var refreshToken = await LoginAndGetRefreshTokenAsync();
        var newRefreshToken = ReadRefreshToken(await RefreshAsync(refreshToken));

        var response = await RefreshAsync(newRefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ReusedToken_RevokesWholeFamily()
    {
        await CreateUserAsync(_employee);
        var stolenToken = await LoginAndGetRefreshTokenAsync();
        var legitimateToken = ReadRefreshToken(await RefreshAsync(stolenToken));

        var reuseResponse = await RefreshAsync(stolenToken);
        var legitimateResponse = await RefreshAsync(legitimateToken);

        await AssertRefreshFailedAsync(reuseResponse);
        await AssertRefreshFailedAsync(legitimateResponse);
    }

    [Fact]
    public async Task Refresh_ReusedToken_KeepsOtherSessionsOfTheUser()
    {
        await CreateUserAsync(_employee);
        var otherSessionToken = await LoginAndGetRefreshTokenAsync();
        var stolenToken = await LoginAndGetRefreshTokenAsync();
        await RefreshAsync(stolenToken);

        await RefreshAsync(stolenToken);
        var response = await RefreshAsync(otherSessionToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
    {
        var user = await CreateUserAsync(_employee);
        const string expiredToken = "expired-token-value";
        await ArrangeAsync(dbContext => dbContext.RefreshTokens.Add(RefreshToken.Create(
            user.Id,
            Guid.NewGuid(),
            RefreshTokenService.Hash(expiredToken),
            DateTimeOffset.UtcNow.AddDays(-8),
            TimeSpan.FromDays(7))));

        var response = await RefreshAsync(expiredToken);

        await AssertRefreshFailedAsync(response);
    }

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsUnauthorizedAndDeletesCookie()
    {
        var response = await RefreshAsync("unknown-token-value");

        await AssertRefreshFailedAsync(response);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => IsDeletion(cookie));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/auth/refresh", content: null, CancellationToken);

        await AssertRefreshFailedAsync(response);
    }

    [Fact]
    public async Task Logout_ValidToken_RevokesTokenAndDeletesCookie()
    {
        await CreateUserAsync(_employee);
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        var response = await SendWithCookieAsync("/api/auth/logout", refreshToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => IsDeletion(cookie));
        await AssertRefreshFailedAsync(await RefreshAsync(refreshToken));
    }

    [Fact]
    public async Task Logout_WithoutCookie_ReturnsNoContent()
    {
        var response = await _client.PostAsync("/api/auth/logout", content: null, CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteExpired_MixedTokens_DeletesOnlyExpiredOnes()
    {
        var user = await CreateUserAsync(_employee);
        var activeToken = await LoginAndGetRefreshTokenAsync();
        await ArrangeAsync(dbContext => dbContext.RefreshTokens.Add(RefreshToken.Create(
            user.Id,
            Guid.NewGuid(),
            RefreshTokenService.Hash("expired-token-value"),
            DateTimeOffset.UtcNow.AddDays(-8),
            TimeSpan.FromDays(7))));

        await using var scope = CreateScope();
        var deleted = await scope.ServiceProvider.GetRequiredService<RefreshTokenService>().DeleteExpiredAsync(CancellationToken);

        Assert.Equal(1, deleted);
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var remaining = await dbContext.RefreshTokens.SingleAsync(CancellationToken);
        Assert.Equal(RefreshTokenService.Hash(activeToken), remaining.TokenHash);
    }

    private async Task<string> LoginAndGetRefreshTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(_employee.Email, Password), CancellationToken);
        response.EnsureSuccessStatusCode();
        return ReadRefreshToken(response);
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        SendWithCookieAsync("/api/auth/refresh", refreshToken);

    private async Task<HttpResponseMessage> SendWithCookieAsync(string path, string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Cookie", $"{RefreshTokenCookie.Name}={refreshToken}");
        return await _client.SendAsync(request, CancellationToken);
    }

    private async Task<(RefreshToken Old, RefreshToken Current)> LoadAsync(string oldToken, string currentToken)
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var tokens = await dbContext.RefreshTokens.AsNoTracking().ToListAsync(CancellationToken);
        return (
            tokens.Single(token => token.TokenHash.SequenceEqual(RefreshTokenService.Hash(oldToken))),
            tokens.Single(token => token.TokenHash.SequenceEqual(RefreshTokenService.Hash(currentToken))));
    }

    private static string ReadRefreshToken(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith($"{RefreshTokenCookie.Name}=", StringComparison.Ordinal));
        var nameAndValue = cookie.Split(';')[0];
        return nameAndValue[(RefreshTokenCookie.Name.Length + 1)..];
    }

    private static bool IsDeletion(string cookie) =>
        cookie.StartsWith($"{RefreshTokenCookie.Name}=;", StringComparison.Ordinal)
        && cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);

    private static async Task AssertRefreshFailedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(AuthErrors.RefreshFailed, problem?.Extensions["code"]?.ToString());
    }
}
