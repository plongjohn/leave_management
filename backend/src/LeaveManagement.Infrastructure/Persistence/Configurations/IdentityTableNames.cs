using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shorter names than Identity's defaults (<c>AspNetUserClaims</c> …), in line with the rest of
/// the schema. Claims, external logins and user tokens are not used yet, but come with
/// <c>IdentityUserContext</c> and keep the door open for external logins.
/// </summary>
internal static class IdentityTableNames
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
    }
}
