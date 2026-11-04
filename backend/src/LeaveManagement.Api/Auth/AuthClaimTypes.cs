using Microsoft.IdentityModel.JsonWebTokens;

namespace LeaveManagement.Api.Auth;

/// <summary>The claims of an access token (ADR 0004). Short JWT names, no mapping to the long .NET ones.</summary>
internal static class AuthClaimTypes
{
    public const string Subject = JwtRegisteredClaimNames.Sub;
    public const string TokenId = JwtRegisteredClaimNames.Jti;
    public const string EmployeeId = "employee_id";
    public const string Role = "role";
}
