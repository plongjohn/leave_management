namespace LeaveManagement.Api.Auth;

/// <summary>Error codes of the auth endpoints, returned as <c>code</c> in ProblemDetails.</summary>
public static class AuthErrors
{
    public const string LoginFailed = "auth.login_failed";
}
