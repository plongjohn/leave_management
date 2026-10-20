namespace LeaveManagement.Api.Auth;

/// <summary>
/// The refresh token travels only in this cookie (ADR 0004): not readable by scripts, only over
/// HTTPS, never sent cross-site and only to the auth endpoints.
/// </summary>
internal static class RefreshTokenCookie
{
    public const string Name = "refresh_token";
    public const string Path = "/api/auth";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Append(HttpResponse response, IssuedRefreshToken token) =>
        response.Cookies.Append(Name, token.Value, CreateOptions(token.ExpiresAt));

    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, CreateOptions(expiresAt: null));

    private static CookieOptions CreateOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true,
    };
}
