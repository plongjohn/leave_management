namespace LeaveManagement.Api.Auth;

/// <summary>The plain token value, sent to the client once and never stored.</summary>
/// <param name="IsPersistent">Whether the cookie should survive closing the browser ("stay signed in").</param>
internal sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAt, bool IsPersistent);
