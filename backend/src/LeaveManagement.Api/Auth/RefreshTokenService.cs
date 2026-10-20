using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using LeaveManagement.Infrastructure.Identity;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeaveManagement.Api.Auth;

/// <summary>Issues, rotates and revokes refresh tokens (ADR 0004).</summary>
internal sealed partial class RefreshTokenService(
    LeaveManagementDbContext dbContext,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    ILogger<RefreshTokenService> logger)
{
    // 256 bits of randomness.
    private const int TokenBytes = 32;

    /// <summary>Starts a new token family, i.e. a new session.</summary>
    /// <param name="isPersistent">"Stay signed in": the cookie survives closing the browser.</param>
    public async Task<IssuedRefreshToken> IssueAsync(Guid userId, bool isPersistent, CancellationToken cancellationToken)
    {
        var issued = Add(userId, familyId: Guid.CreateVersion7(), isPersistent, out _);
        await dbContext.SaveChangesAsync(cancellationToken);
        return issued;
    }

    /// <summary>
    /// Exchanges a valid token for a new one of the same family. Presenting an already revoked
    /// token means it was copied: the whole family is revoked, so both holders are signed out.
    /// </summary>
    /// <returns>The user and the new token, or <c>null</c> if the token is unknown, expired or revoked.</returns>
    public async Task<RefreshTokenRotation?> RotateAsync(string value, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var current = await FindAsync(value, cancellationToken);
        if (current is null || current.IsExpired(now))
        {
            return null;
        }

        if (current.IsRevoked)
        {
            await RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
            LogReuseDetected(current.UserId, current.FamilyId);
            return null;
        }

        var issued = Add(current.UserId, current.FamilyId, current.IsPersistent, out var next);
        current.Revoke(now, next.Id);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A parallel request rotated the same token first; that request got the new token.
            return null;
        }

        return new RefreshTokenRotation(current.UserId, issued);
    }

    /// <summary>Ends the session of the token; unknown or already revoked tokens are ignored.</summary>
    public async Task RevokeAsync(string value, CancellationToken cancellationToken)
    {
        var token = await FindAsync(value, cancellationToken);
        if (token is null || token.IsRevoked)
        {
            return;
        }

        token.Revoke(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Expired tokens can no longer be used, so they are not needed for reuse detection either.
    /// </summary>
    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await dbContext.RefreshTokens
            .Where(token => token.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>Only the hash is stored, so a database leak does not expose usable tokens.</summary>
    public static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private IssuedRefreshToken Add(Guid userId, Guid familyId, bool isPersistent, out RefreshToken token)
    {
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        token = RefreshToken.Create(
            userId,
            familyId,
            Hash(value),
            isPersistent,
            timeProvider.GetUtcNow(),
            options.Value.RefreshTokenLifetime);
        dbContext.RefreshTokens.Add(token);
        return new IssuedRefreshToken(value, token.ExpiresAt, isPersistent);
    }

    private async Task<RefreshToken?> FindAsync(string value, CancellationToken cancellationToken)
    {
        var hash = Hash(value);
        return await dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Revoked refresh token reused for user {UserId}; revoked token family {FamilyId}")]
    private partial void LogReuseDetected(Guid userId, Guid familyId);
}
