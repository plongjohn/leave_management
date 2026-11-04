using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace LeaveManagement.Api.Auth;

/// <summary>
/// Limits login attempts per client IP address. Identity's lockout protects a single account;
/// this also slows down trying one password on many accounts.
/// </summary>
internal static class LoginRateLimiting
{
    public const string PolicyName = "login";
    public const string SectionName = "RateLimiting:Login";

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(SectionName).Get<LoginRateLimitOptions>() ?? new LoginRateLimitOptions();

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(PolicyName, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = settings.Window,
                    QueueLimit = 0,
                }));
        });
    }

    private sealed class LoginRateLimitOptions
    {
        public int PermitLimit { get; set; } = 10;

        public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
    }
}
