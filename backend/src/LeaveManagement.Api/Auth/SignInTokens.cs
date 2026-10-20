namespace LeaveManagement.Api.Auth;

internal sealed record SignInTokens(AccessToken AccessToken, IssuedRefreshToken RefreshToken);
