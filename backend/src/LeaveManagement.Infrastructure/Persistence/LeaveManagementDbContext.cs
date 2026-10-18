using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Holidays;
using LeaveManagement.Domain.LeaveRequests;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence;

public sealed class LeaveManagementDbContext(DbContextOptions<LeaveManagementDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<LeaveEntitlement> LeaveEntitlements => Set<LeaveEntitlement>();

    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeaveManagementDbContext).Assembly);
    }
}
