namespace LeaveManagement.Api.Employees;

public sealed record EmployeeListItemResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly EntryDate);
