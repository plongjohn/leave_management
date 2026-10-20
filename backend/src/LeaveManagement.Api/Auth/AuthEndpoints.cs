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
        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimiting.PolicyName)
            .WithValidation<LoginRequest>();

        endpoints.MapGet("/api/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .WithTags("Auth")
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<Results<Ok<AccessTokenResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        AuthService authService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var accessToken = await authService.LoginAsync(request, cancellationToken);
        if (accessToken is null)
        {
            return LoginFailed();
        }

        return TypedResults.Ok(ToResponse(accessToken, timeProvider));
    }

    private static async Task<Results<Ok<CurrentUserResponse>, NotFound>> GetCurrentUserAsync(
        HttpContext httpContext,
        AuthService authService,
        CancellationToken cancellationToken)
    {
        var currentUser = await authService.GetCurrentUserAsync(httpContext.User.GetEmployeeId(), cancellationToken);
        return currentUser is null ? TypedResults.NotFound() : TypedResults.Ok(currentUser);
    }

    // Same answer for unknown email, wrong password and lockout (ADR 0004).
    private static ProblemHttpResult LoginFailed() =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Login failed.",
            detail: "The email or the password is wrong, or the account is temporarily locked.",
            extensions: new Dictionary<string, object?> { ["code"] = AuthErrors.LoginFailed });

    private static AccessTokenResponse ToResponse(AccessToken accessToken, TimeProvider timeProvider)
    {
        var expiresIn = accessToken.ExpiresAt - timeProvider.GetUtcNow();
        return new AccessTokenResponse(accessToken.Value, "Bearer", (int)expiresIn.TotalSeconds);
    }
}
