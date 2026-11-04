using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Employees;

internal sealed class EmployeeQueries(LeaveManagementDbContext dbContext)
{
    public async Task<IReadOnlyList<EmployeeListItemResponse>> GetListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(employee => new EmployeeListItemResponse(
                employee.Id,
                employee.FirstName,
                employee.LastName,
                employee.Email,
                employee.EntryDate))
            .ToListAsync(cancellationToken);
    }
}
