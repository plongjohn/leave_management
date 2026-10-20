namespace LeaveManagement.Api.Auth;

internal sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
