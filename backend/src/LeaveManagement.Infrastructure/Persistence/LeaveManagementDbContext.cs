using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Holidays;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Infrastructure.Identity;
using LeaveManagement.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

/// <summary>
/// One context for the domain and the logins, so users can reference employees with a foreign
/// key and share migrations and transactions. <c>IdentityUserContext</c> instead of
/// <c>IdentityDbContext</c>: roles belong to the employee (ADR 0004), so no role tables.
/// </summary>
public sealed class LeaveManagementDbContext(DbContextOptions<LeaveManagementDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<LeaveEntitlement> LeaveEntitlements => Set<LeaveEntitlement>();

    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity's configuration first, so our configurations can refine it.
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(LeaveManagementDbContext).Assembly);
        IdentityTableNames.Apply(builder);
    }
}
