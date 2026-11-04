using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Domain.Tests.TestData;

internal static class TestEmployees
{
    public static Employee Manager() =>
        Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);

    public static Employee ReportOf(Employee manager, DateOnly? entryDate = null) =>
        Employee.Create(
            "Lena",
            "Wagner",
            "lena.wagner@example.at",
            entryDate ?? new DateOnly(2020, 2, 1),
            EmployeeRole.Employee,
            manager.Id);

    public static Employee Colleague() =>
        Employee.Create("Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15), EmployeeRole.Employee);

    public static Employee Hr() =>
        Employee.Create("Sabine", "Huber", "sabine.huber@example.at", new DateOnly(2015, 3, 1), EmployeeRole.Hr);

    /// <summary>An employee without a manager, e.g. the managing director.</summary>
    public static Employee WithoutManager(EmployeeRole role = EmployeeRole.Manager) =>
        Employee.Create("Andrea", "Steiner", "andrea.steiner@example.at", new DateOnly(2012, 1, 2), role);
}
