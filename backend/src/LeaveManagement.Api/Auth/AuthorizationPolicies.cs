using LeaveManagement.Domain.Employees;
using Microsoft.AspNetCore.Authorization;

namespace LeaveManagement.Api.Auth;

/// <summary>
/// Role-based access to endpoints. Rules that depend on the data (e.g. "only the own manager
/// decides") are checked in the domain, not here.
/// </summary>
internal static class AuthorizationPolicies
{
    /// <summary>Approvals: managers decide for their reports, HR for employees without a manager.</summary>
    public const string ManagerOrHr = "ManagerOrHr";

    /// <summary>Entitlements, holidays and other administration.</summary>
    public const string Hr = "Hr";

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Deny by default: every endpoint needs a signed-in user unless it allows anonymous access.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(ManagerOrHr, policy => policy.RequireRole(nameof(EmployeeRole.Manager), nameof(EmployeeRole.Hr)))
            .AddPolicy(Hr, policy => policy.RequireRole(nameof(EmployeeRole.Hr)));

        return services;
    }
}
