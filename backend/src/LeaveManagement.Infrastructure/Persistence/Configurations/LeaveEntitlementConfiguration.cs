using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

internal sealed class LeaveEntitlementConfiguration : IEntityTypeConfiguration<LeaveEntitlement>
{
    public void Configure(EntityTypeBuilder<LeaveEntitlement> builder)
    {
        builder.HasKey(entitlement => entitlement.Id);
        builder.Property(entitlement => entitlement.Id).ValueGeneratedNever();

        builder.Property(entitlement => entitlement.Days).HasPrecision(ColumnLengths.DaysPrecision, ColumnLengths.DaysScale);

        builder.Ignore(entitlement => entitlement.ExpiresOn);

        // The balance calculation relies on at most one entitlement per employee and year.
        builder.HasIndex(entitlement => new { entitlement.EmployeeId, entitlement.Year }).IsUnique();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(entitlement => entitlement.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
