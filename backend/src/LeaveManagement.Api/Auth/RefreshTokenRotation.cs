namespace LeaveManagement.Api.Auth;

internal sealed record RefreshTokenRotation(Guid UserId, IssuedRefreshToken NewToken);
