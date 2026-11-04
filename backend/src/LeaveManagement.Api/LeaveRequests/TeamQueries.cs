using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>
/// The team of an employee: colleagues with the same manager, the manager and the employee's own
/// reports (decision 2026-11-03). Used for "who else is away" here and for the team calendar.
/// </summary>
internal sealed class TeamQueries(LeaveManagementDbContext dbContext)
{
    public async Task<IReadOnlyList<Guid>> GetTeamMemberIdsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var managerId = await dbContext.Employees
            .Where(employee => employee.Id == employeeId)
            .Select(employee => employee.ManagerId)
            .SingleAsync(cancellationToken);

        return await dbContext.Employees
            .Where(employee => employee.Id != employeeId
                && ((managerId != null && (employee.ManagerId == managerId || employee.Id == managerId))
                    || employee.ManagerId == employeeId))
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Team members with a pending or approved absence that overlaps <paramref name="period"/>.</summary>
    public async Task<IReadOnlyList<AbsentColleagueResponse>> GetAbsentColleaguesAsync(
        Guid employeeId,
        DateRange period,
        CancellationToken cancellationToken)
    {
        var teamMemberIds = await GetTeamMemberIdsAsync(employeeId, cancellationToken);

        var absentIds = dbContext.LeaveRequests
            .Where(request => teamMemberIds.Contains(request.EmployeeId)
                && (request.Status == LeaveRequestStatus.Pending || request.Status == LeaveRequestStatus.Approved)
                && request.StartDate <= period.End
                && request.EndDate >= period.Start)
            .Select(request => request.EmployeeId);

        return await dbContext.Employees
            .Where(employee => absentIds.Contains(employee.Id))
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .Select(employee => new AbsentColleagueResponse(employee.Id, employee.FirstName + " " + employee.LastName))
            .ToListAsync(cancellationToken);
    }
}
