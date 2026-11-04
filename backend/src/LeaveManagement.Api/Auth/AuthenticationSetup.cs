using LeaveManagement.Infrastructure.Identity;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LeaveManagement.Api.Auth;

internal static class AuthenticationSetup
{
    private const int MaxFailedLoginAttempts = 5;

    // NIST SP 800-63B: length instead of composition rules.
    private const int MinimumPasswordLength = 12;

    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Small tolerance for clock differences; the default of five minutes is long for 15-minute tokens.
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddIdentityStore(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = MinimumPasswordLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = MaxFailedLoginAttempts;
                options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
            })
            .AddSignInManager()
            .AddEntityFrameworkStores<LeaveManagementDbContext>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured lazily from the validated options, so tools that only build the host
        // (e.g. dotnet ef) work without a signing key.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var settings = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = settings.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = AuthClaimTypes.Subject,
                    RoleClaimType = AuthClaimTypes.Role,
                    ClockSkew = ClockSkew,
                };
            });

        services.AddScoped<AccessTokenIssuer>();
        return services;
    }
}
