namespace LeaveManagement.Infrastructure.Identity;

/// <summary>
/// A refresh token as stored: only the SHA-256 hash of the value the client holds (ADR 0004).
/// All tokens of one login share a <see cref="FamilyId"/>; rotation replaces one token by the next.
/// </summary>
public sealed class RefreshToken
{
    // Used by EF Core when materializing; parameter names match the property names.
    private RefreshToken(Guid id, Guid userId, Guid familyId, byte[] tokenHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public byte[] TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>The token issued in exchange for this one when it was rotated.</summary>
    public Guid? ReplacedById { get; private set; }

    /// <summary>Concurrency token (PostgreSQL <c>xmin</c>): only one of two parallel rotations succeeds.</summary>
    public uint Version { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static RefreshToken Create(Guid userId, Guid familyId, byte[] tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        var createdAt = now.ToUniversalTime();
        return new RefreshToken(Guid.CreateVersion7(), userId, familyId, tokenHash, createdAt, createdAt + lifetime);
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    public void Revoke(DateTimeOffset now, Guid? replacedById = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = now.ToUniversalTime();
        ReplacedById = replacedById;
    }
}
