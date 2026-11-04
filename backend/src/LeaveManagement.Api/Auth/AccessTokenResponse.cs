namespace LeaveManagement.Api.Auth;

/// <summary>Shaped like an OAuth 2.0 token response (RFC 6749, section 5.1).</summary>
/// <param name="ExpiresIn">Lifetime of the access token in seconds.</param>
public sealed record AccessTokenResponse(string AccessToken, string TokenType, int ExpiresIn);
