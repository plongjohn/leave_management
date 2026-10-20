using System.Security.Claims;

namespace LeaveManagement.Api.Auth;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>Only for authenticated users; every access token carries the employee id.</summary>
    public static Guid GetEmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.EmployeeId);
        return Guid.TryParse(value, out var employeeId)
            ? employeeId
            : throw new InvalidOperationException("The access token has no valid employee id.");
    }
}
