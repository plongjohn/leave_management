using LeaveManagement.Infrastructure.Identity;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Auth;

internal sealed partial class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    LeaveManagementDbContext dbContext,
    AccessTokenIssuer accessTokenIssuer,
    ILogger<AuthService> logger)
{
    // Hashed once; verifying against it costs as much as checking a real password.
    private static readonly Lazy<string> UnknownUserPasswordHash =
        new(() => new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), Guid.NewGuid().ToString()));

    /// <returns>The access token, or <c>null</c> if the email, the password or the lockout prevents the login.</returns>
    public async Task<AccessToken?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Takes as long as a wrong password, so the response time does not reveal which emails exist.
            userManager.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), UnknownUserPasswordHash.Value, request.Password);
            return null;
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            LogLoginFailed(user.Id, result.IsLockedOut);
            return null;
        }

        return await IssueAccessTokenAsync(user, cancellationToken);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => new CurrentUserResponse(
                employee.Id,
                employee.FirstName,
                employee.LastName,
                employee.Email,
                employee.Role))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<AccessToken> IssueAccessTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        // Read at every token issue, so a changed role applies with the next token.
        var role = await dbContext.Employees
            .Where(employee => employee.Id == user.EmployeeId)
            .Select(employee => employee.Role)
            .SingleAsync(cancellationToken);

        return accessTokenIssuer.Create(user.Id, user.EmployeeId, role);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Login failed for user {UserId} (locked out: {IsLockedOut})")]
    private partial void LogLoginFailed(Guid userId, bool isLockedOut);
}
