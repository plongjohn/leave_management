using LeaveManagement.Domain.Employees;
using Microsoft.AspNetCore.Identity;

namespace LeaveManagement.Infrastructure.Identity;

/// <summary>
/// The login of an employee: user name, password hash, lockout (ADR 0004). Login details stay
/// here; the domain entity <see cref="Employee"/> knows nothing about them.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid EmployeeId { get; set; }

    /// <summary>The employee's email is the user name; normalization is done by Identity's UserManager.</summary>
    public static ApplicationUser ForEmployee(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        return new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = employee.Email,
            Email = employee.Email,
            EmployeeId = employee.Id,
        };
    }
}
