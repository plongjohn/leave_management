using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.Employees;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Api.Tests.Employees;

public sealed class GetEmployeesTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task GetEmployees_WithEmployees_ReturnsThemSortedByLastName()
    {
        await ArrangeAsync(dbContext => dbContext.Employees.AddRange(
            Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee),
            Employee.Create("Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15), EmployeeRole.Employee),
            Employee.Create("Anna", "Bauer", "anna.bauer@example.at", new DateOnly(2022, 4, 1), EmployeeRole.Employee)));

        var response = await Client.GetAsync("/api/employees", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeListItemResponse>>(CancellationToken);
        Assert.NotNull(employees);
        Assert.Equal(
            ["Anna Bauer", "Thomas Bauer", "Lena Wagner"],
            employees.Select(employee => $"{employee.FirstName} {employee.LastName}"));
    }

    [Fact]
    public async Task GetEmployees_NoEmployees_ReturnsEmptyList()
    {
        var response = await Client.GetAsync("/api/employees", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeListItemResponse>>(CancellationToken);
        Assert.NotNull(employees);
        Assert.Empty(employees);
    }
}
