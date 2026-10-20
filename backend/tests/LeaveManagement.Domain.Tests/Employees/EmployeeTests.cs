using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Domain.Tests.Employees;

public sealed class EmployeeTests
{
    [Fact]
    public void Create_ValidInput_SetsRoleAndNormalizesEmail()
    {
        var employee = Employee.Create(" Lena ", "Wagner", "Lena.Wagner@Example.at", new DateOnly(2020, 2, 1), EmployeeRole.Hr);

        Assert.Equal("Lena", employee.FirstName);
        Assert.Equal("lena.wagner@example.at", employee.Email);
        Assert.Equal(EmployeeRole.Hr, employee.Role);
    }

    [Fact]
    public void Create_UnknownRole_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), (EmployeeRole)99));
    }
}
