namespace LeaveManagement.Api.Auth;

/// <summary>The plain token value, sent to the client once and never stored.</summary>
internal sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAt);
