using LeaveManagement.Domain.Employees;
using LeaveManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Adds the link to the employee; everything else is configured by Identity's
/// <c>IdentityUserContext</c>. Runs after the base configuration (see the DbContext).
/// </summary>
internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");

        builder.Property(user => user.Id).ValueGeneratedNever();

        // Identity names these indexes "UserNameIndex" and "EmailIndex"; renamed to match the schema.
        builder.HasIndex(user => user.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name");
        builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("ix_users_normalized_email");

        // One login per employee.
        builder.HasIndex(user => user.EmployeeId).IsUnique();

        builder.HasOne<Employee>()
            .WithOne()
            .HasForeignKey<ApplicationUser>(user => user.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
