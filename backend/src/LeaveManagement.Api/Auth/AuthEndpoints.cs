using LeaveManagement.Api.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaveManagement.Api.Auth;

internal static class AuthEndpoints
{
    public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityStore();
        services.AddJwtAuthentication(configuration);
        services.AddLoginRateLimiting(configuration);
        services.AddAuthorization();
        services.AddScoped<AuthService>();
        services.AddScoped<RefreshTokenService>();
        services.AddHostedService<RefreshTokenCleanupService>();
        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Login, refresh and logout work without an access token: it may have expired.
        var auth = endpoints.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        auth.MapPost("/login", LoginAsync)
            .WithName("Login")
            .RequireRateLimiting(LoginRateLimiting.PolicyName)
            .WithValidation<LoginRequest>();

        auth.MapPost("/refresh", RefreshAsync).WithName("RefreshToken");

        auth.MapPost("/logout", LogoutAsync).WithName("Logout");

        endpoints.MapGet("/api/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithTags("Auth")
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<Results<Ok<AccessTokenResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        AuthService authService,
        HttpResponse response,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var tokens = await authService.LoginAsync(request, cancellationToken);
        if (tokens is null)
        {
            // Same answer for unknown email, wrong password and lockout (ADR 0004).
            return Unauthorized(
                AuthErrors.LoginFailed,
                "Login failed.",
                "The email or the password is wrong, or the account is temporarily locked.");
        }

        RefreshTokenCookie.Append(response, tokens.RefreshToken);
        return TypedResults.Ok(ToResponse(tokens.AccessToken, timeProvider));
    }

    private static async Task<Results<Ok<AccessTokenResponse>, ProblemHttpResult>> RefreshAsync(
        HttpRequest request,
        HttpResponse response,
        AuthService authService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(request);
        var tokens = refreshToken is null ? null : await authService.RefreshAsync(refreshToken, cancellationToken);
        if (tokens is null)
        {
            RefreshTokenCookie.Delete(response);
            return Unauthorized(AuthErrors.RefreshFailed, "Session expired.", "Please sign in again.");
        }

        RefreshTokenCookie.Append(response, tokens.RefreshToken);
        return TypedResults.Ok(ToResponse(tokens.AccessToken, timeProvider));
    }

    private static async Task<NoContent> LogoutAsync(
        HttpRequest request,
        HttpResponse response,
        AuthService authService,
        CancellationToken cancellationToken)
    {
        if (RefreshTokenCookie.Read(request) is { } refreshToken)
        {
            await authService.LogoutAsync(refreshToken, cancellationToken);
        }

        RefreshTokenCookie.Delete(response);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<CurrentUserResponse>, NotFound>> GetCurrentUserAsync(
        HttpContext httpContext,
        AuthService authService,
        CancellationToken cancellationToken)
    {
        var currentUser = await authService.GetCurrentUserAsync(httpContext.User.GetEmployeeId(), cancellationToken);
        return currentUser is null ? TypedResults.NotFound() : TypedResults.Ok(currentUser);
    }

    private static ProblemHttpResult Unauthorized(string code, string title, string detail) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: title,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    private static AccessTokenResponse ToResponse(AccessToken accessToken, TimeProvider timeProvider)
    {
        var expiresIn = accessToken.ExpiresAt - timeProvider.GetUtcNow();
        return new AccessTokenResponse(accessToken.Value, "Bearer", (int)expiresIn.TotalSeconds);
    }
}
