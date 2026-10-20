using Microsoft.Extensions.Options;

namespace LeaveManagement.Api.Auth;

/// <summary>Runs at start-up, so the API refuses to start without a usable signing key (ADR 0004).</summary>
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is missing.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is missing.");
        }

        if (!IsUsableSigningKey(options.SigningKey))
        {
            failures.Add(
                $"Jwt:SigningKey is missing or invalid. Set the environment variable Jwt__SigningKey to a " +
                $"base64-encoded random key of at least {JwtOptions.MinimumSigningKeyBytes} bytes (see database/README.md).");
        }

        if (options.AccessTokenLifetime <= TimeSpan.Zero)
        {
            failures.Add("Jwt:AccessTokenLifetime must be positive.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsUsableSigningKey(string signingKey)
    {
        var buffer = new byte[signingKey.Length];
        return Convert.TryFromBase64String(signingKey, buffer, out var length)
               && length >= JwtOptions.MinimumSigningKeyBytes;
    }
}
