using LeaveManagement.Domain.Employees;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LeaveManagement.Api.Auth;

internal sealed class AccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessToken Create(Guid userId, Guid employeeId, EmployeeRole role)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now + settings.AccessTokenLifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [AuthClaimTypes.Subject] = userId.ToString(),
                [AuthClaimTypes.EmployeeId] = employeeId.ToString(),
                [AuthClaimTypes.Role] = role.ToString(),
                [AuthClaimTypes.TokenId] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAt);
    }
}
