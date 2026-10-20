using Microsoft.IdentityModel.Tokens;

namespace LeaveManagement.Api.Auth;

/// <summary>Settings for issuing and validating access tokens (section <c>Jwt</c>).</summary>
internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // HS256 needs a key of at least 256 bits.
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Base64-encoded secret; comes from the environment variable <c>Jwt__SigningKey</c>, never from Git.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public SymmetricSecurityKey CreateSigningKey() => new(Convert.FromBase64String(SigningKey));
}
