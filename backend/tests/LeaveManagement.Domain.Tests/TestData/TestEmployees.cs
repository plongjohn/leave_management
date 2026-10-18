using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Domain.Tests.TestData;

internal static class TestEmployees
{
    public static Employee Manager() =>
        Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1));

    public static Employee ReportOf(Employee manager, DateOnly? entryDate = null) =>
        Employee.Create("Lena", "Wagner", "lena.wagner@example.at", entryDate ?? new DateOnly(2020, 2, 1), manager.Id);

    public static Employee Colleague() =>
        Employee.Create("Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15));
}
