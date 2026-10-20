using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LeaveManagement.Api.Tests.Infrastructure;

/// <summary>Builds access tokens by hand, to test how the API treats invalid ones.</summary>
internal static class TestTokens
{
    public static string Create(
        string signingKey,
        Guid employeeId,
        DateTime expiresAt,
        string role = "Employee",
        string issuer = LeaveManagementApiFactory.Issuer)
    {
        var issuedAt = expiresAt.AddMinutes(-15);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = LeaveManagementApiFactory.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = Guid.NewGuid().ToString(),
                ["employee_id"] = employeeId.ToString(),
                ["role"] = role,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static string RandomSigningKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
