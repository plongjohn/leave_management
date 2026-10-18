using LeaveManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Demo data for the local development database. Registered through EF Core's seeding hooks
/// in the Development environment only, so it never ends up in migrations or production.
/// </summary>
internal static class DevelopmentDataSeeder
{
    public static void Seed(DbContext context)
    {
        if (context.Set<Employee>().Any())
        {
            return;
        }

        context.Set<Employee>().AddRange(CreateEmployees());
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<Employee>().AnyAsync(cancellationToken))
        {
            return;
        }

        context.Set<Employee>().AddRange(CreateEmployees());
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<Employee> CreateEmployees()
    {
        var hrLead = Employee.Create("Sabine", "Huber", "sabine.huber@example.at", new DateOnly(2015, 3, 1));
        var developmentLead = Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1));

        return
        [
            hrLead,
            developmentLead,
            Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), developmentLead.Id),
            Employee.Create("Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15), developmentLead.Id),
            Employee.Create("Julia", "Pichler", "julia.pichler@example.at", new DateOnly(2023, 1, 9), developmentLead.Id),
            Employee.Create("Stefan", "Moser", "stefan.moser@example.at", new DateOnly(2019, 11, 4), hrLead.Id),
        ];
    }
}
