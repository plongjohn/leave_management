using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Infrastructure.Identity;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.Persistence;

/// <summary>Checks the link between logins and employees in the database schema.</summary>
public sealed class ApplicationUserPersistenceTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    private readonly Employee _employee =
        Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee);

    [Fact]
    public async Task SaveChanges_UserOfEmployee_RoundTripsEmployeeId()
    {
        var user = ApplicationUser.ForEmployee(_employee);
        await ArrangeAsync(dbContext =>
        {
            dbContext.Employees.Add(_employee);
            dbContext.Users.Add(user);
        });

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var loaded = await dbContext.Users.SingleAsync(entry => entry.Id == user.Id, CancellationToken);

        Assert.Equal(_employee.Id, loaded.EmployeeId);
        Assert.Equal("lena.wagner@example.at", loaded.UserName);
    }

    [Fact]
    public async Task SaveChanges_SecondUserForSameEmployee_Throws()
    {
        await ArrangeAsync(dbContext =>
        {
            dbContext.Employees.Add(_employee);
            dbContext.Users.Add(ApplicationUser.ForEmployee(_employee));
        });

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var secondUser = ApplicationUser.ForEmployee(_employee);
        secondUser.UserName = "lena.second@example.at";
        dbContext.Users.Add(secondUser);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync(CancellationToken));
    }

    [Fact]
    public async Task SaveChanges_UserWithoutEmployee_Throws()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        dbContext.Users.Add(ApplicationUser.ForEmployee(_employee));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync(CancellationToken));
    }
}
