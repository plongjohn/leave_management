using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Holidays;
using LeaveManagement.Domain.LeaveRequests;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Demo data for the local development database. Registered through EF Core's seeding hooks
/// in the Development environment only, so it never ends up in migrations or production.
/// Each part is seeded on its own, so an existing database receives parts added later.
/// </summary>
internal static class DevelopmentDataSeeder
{
    private const int DaysPerWeek = 7;

    public static void Seed(DbContext context)
    {
        if (!context.Set<Employee>().Any())
        {
            context.Set<Employee>().AddRange(CreateEmployees());
            context.SaveChanges();
        }

        if (!context.Set<LeaveEntitlement>().Any())
        {
            AddLeaveData(context, context.Set<Employee>().ToList(), context.Set<Holiday>().ToList());
            context.SaveChanges();
        }
    }

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (!await context.Set<Employee>().AnyAsync(cancellationToken))
        {
            context.Set<Employee>().AddRange(CreateEmployees());
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.Set<LeaveEntitlement>().AnyAsync(cancellationToken))
        {
            var employees = await context.Set<Employee>().ToListAsync(cancellationToken);
            var holidays = await context.Set<Holiday>().ToListAsync(cancellationToken);
            AddLeaveData(context, employees, holidays);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static List<Employee> CreateEmployees()
    {
        // The managing director has no manager, so HR decides on her requests.
        var director = Employee.Create(
            "Andrea", "Steiner", "andrea.steiner@example.at", new DateOnly(2012, 1, 2), EmployeeRole.Manager);
        var hrLead = Employee.Create(
            "Sabine", "Huber", "sabine.huber@example.at", new DateOnly(2015, 3, 1), EmployeeRole.Hr, director.Id);
        var developmentLead = Employee.Create(
            "Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager, director.Id);

        return
        [
            director,
            hrLead,
            developmentLead,
            Employee.Create(
                "Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee, developmentLead.Id),
            Employee.Create(
                "Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15), EmployeeRole.Employee, developmentLead.Id),
            Employee.Create(
                "Julia", "Pichler", "julia.pichler@example.at", new DateOnly(2023, 1, 9), EmployeeRole.Employee, developmentLead.Id),
            Employee.Create(
                "Stefan", "Moser", "stefan.moser@example.at", new DateOnly(2019, 11, 4), EmployeeRole.Employee, hrLead.Id),
        ];
    }

    // Entitlements for last, this and next year, and a few requests in every status relative to today,
    // created through the domain so that the demo data follows the business rules.
    private static void AddLeaveData(DbContext context, List<Employee> employees, List<Holiday> holidays)
    {
        var now = TimeProvider.System.GetLocalNow();
        var today = DateOnly.FromDateTime(now.DateTime);
        var nextMonday = today.AddDays(DaysPerWeek - (((int)today.DayOfWeek + 6) % DaysPerWeek));
        var calendar = new WorkingDayCalendar(holidays);

        var entitlements = employees
            .SelectMany(employee => Enumerable.Range(today.Year - 1, 3)
                .Select(year => LeaveEntitlement.Create(employee.Id, year)))
            .ToList();
        context.Set<LeaveEntitlement>().AddRange(entitlements);

        var employeesByEmail = employees.ToDictionary(employee => employee.Email);
        var andrea = employeesByEmail["andrea.steiner@example.at"];
        var markus = employeesByEmail["markus.gruber@example.at"];
        var lena = employeesByEmail["lena.wagner@example.at"];
        var thomas = employeesByEmail["thomas.bauer@example.at"];
        var julia = employeesByEmail["julia.pichler@example.at"];
        var stefan = employeesByEmail["stefan.moser@example.at"];

        var requests = new List<LeaveRequest>();

        LeaveRequest Submit(Employee employee, DateOnly start, DateOnly end, LeaveType type, SpecialLeaveReason? reason = null)
        {
            var submissionContext = new SubmissionContext(
                calendar,
                [.. requests.Where(request => request.EmployeeId == employee.Id)],
                [.. entitlements.Where(entitlement => entitlement.EmployeeId == employee.Id)],
                today,
                now);
            var request = LeaveRequest.Submit(
                employee, new LeaveRequestDraft(new DateRange(start, end), type, SpecialLeaveReason: reason), submissionContext);
            requests.Add(request);
            return request;
        }

        Submit(lena, nextMonday.AddDays(14), nextMonday.AddDays(18), LeaveType.Vacation).Approve(markus, lena, now);
        Submit(lena, nextMonday.AddDays(35), nextMonday.AddDays(35), LeaveType.TimeOffInLieu);
        Submit(thomas, nextMonday.AddDays(28), nextMonday.AddDays(39), LeaveType.Vacation);
        Submit(julia, nextMonday.AddDays(21), nextMonday.AddDays(23), LeaveType.Vacation).Reject(markus, julia, now);
        Submit(julia, nextMonday.AddDays(7), nextMonday.AddDays(8), LeaveType.SpecialLeave, SpecialLeaveReason.Relocation);
        Submit(stefan, nextMonday.AddDays(-14), nextMonday.AddDays(-12), LeaveType.SickLeave);
        Submit(andrea, nextMonday.AddDays(42), nextMonday.AddDays(46), LeaveType.Vacation);

        context.Set<LeaveRequest>().AddRange(requests);
    }
}
