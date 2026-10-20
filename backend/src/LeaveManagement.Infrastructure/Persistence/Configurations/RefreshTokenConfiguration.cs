using LeaveManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    // SHA-256 produces 32 bytes.
    private const int TokenHashLength = 32;

    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.Property(token => token.TokenHash).HasMaxLength(TokenHashLength);
        builder.HasIndex(token => token.TokenHash).IsUnique();

        // Reuse detection revokes a whole family, the cleanup deletes by expiry.
        builder.HasIndex(token => token.FamilyId);
        builder.HasIndex(token => token.ExpiresAt);

        builder.Property(token => token.Version).IsRowVersion();

        builder.Ignore(token => token.IsRevoked);

        // Deleting a login ends all its sessions.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
