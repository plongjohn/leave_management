using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Api.Auth;

public sealed record CurrentUserResponse(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string Email,
    EmployeeRole Role);
