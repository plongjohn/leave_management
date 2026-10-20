namespace LeaveManagement.Domain.Employees;

/// <summary>
/// What an employee may do in the application. Managers decide on their reports' requests;
/// HR manages entitlements and decides on requests of employees without a manager.
/// </summary>
public enum EmployeeRole
{
    Employee,
    Manager,
    Hr,
}
