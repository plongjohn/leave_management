namespace LeaveManagement.Api.Auth;

/// <param name="RememberMe">"Stay signed in": keep the session after the browser is closed.</param>
public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);
