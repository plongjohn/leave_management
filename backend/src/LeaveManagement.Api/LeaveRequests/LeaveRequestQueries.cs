using LeaveManagement.Api.Common;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>
/// Reads the signed-in employee's requests and balance. Requests are loaded as entities, not projected,
/// because the returned days and the balance are calculated by the domain.
/// </summary>
internal sealed class LeaveRequestQueries(LeaveManagementDbContext dbContext, CompanyClock clock)
{
    /// <summary>The requests that touch <paramref name="year"/>, the latest first.</summary>
    public async Task<IReadOnlyList<MyLeaveRequestResponse>> GetMineAsync(
        Guid employeeId,
        int year,
        CancellationToken cancellationToken)
    {
        var firstDay = new DateOnly(year, 1, 1);
        var lastDay = new DateOnly(year, 12, 31);

        var requests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(request => request.EmployeeId == employeeId && request.StartDate <= lastDay && request.EndDate >= firstDay)
            .OrderByDescending(request => request.StartDate)
            .ToListAsync(cancellationToken);

        var sickLeaves = await LoadSickLeavesDuringVacationAsync(employeeId, requests, cancellationToken);
        var managerId = await dbContext.Employees
            .Where(employee => employee.Id == employeeId)
            .Select(employee => employee.ManagerId)
            .SingleAsync(cancellationToken);
        var names = await LoadNamesAsync(
            requests.Select(request => request.DecidedById).Append(managerId),
            cancellationToken);

        var today = clock.Today();
        return [.. requests.Select(request => new MyLeaveRequestResponse(
            request.Id,
            request.StartDate,
            request.EndDate,
            request.Type,
            request.SpecialLeaveReason,
            request.WorkingDays,
            request.Type == LeaveType.Vacation ? SickLeaveDuringVacation.ReturnedDays(request, sickLeaves) : 0m,
            request.Status,
            request.Comment,
            request.HasMedicalCertificate,
            request.IsCertificateRevoked,
            request.CreatedAt,
            request.Status == LeaveRequestStatus.Pending ? NameOf(managerId, names) : null,
            NameOf(request.DecidedById, names),
            request.DecidedAt,
            request.CanBeCancelled(today),
            request.Version))];
    }

    public async Task<LeaveBalanceResponse> GetMyBalanceAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var entitlements = await dbContext.LeaveEntitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        // Pending and approved vacation takes days, approved sick leave returns some of them.
        var requests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(request => request.EmployeeId == employeeId
                && (request.Type == LeaveType.Vacation || request.Type == LeaveType.SickLeave)
                && (request.Status == LeaveRequestStatus.Pending || request.Status == LeaveRequestStatus.Approved))
            .ToListAsync(cancellationToken);

        var balance = LeaveBalanceCalculator.Calculate(entitlements, requests);
        var today = clock.Today();

        return new LeaveBalanceResponse(
            [.. balance.Years
                .Where(year => year.ExpiresOn >= today)
                .Select(year => new EntitlementYearResponse(
                    year.Year,
                    year.EntitledDays,
                    year.ApprovedDays,
                    year.PendingDays,
                    year.RemainingDays,
                    year.ExpiresOn))],
            balance.AvailableIn(today.Year) - balance.UncoveredDays);
    }

    // Only sick leave that overlaps one of the loaded vacations can return days.
    private async Task<List<LeaveRequest>> LoadSickLeavesDuringVacationAsync(
        Guid employeeId,
        List<LeaveRequest> requests,
        CancellationToken cancellationToken)
    {
        var vacations = requests.Where(request => request.Type == LeaveType.Vacation).ToList();
        if (vacations.Count == 0)
        {
            return [];
        }

        var from = vacations.Min(vacation => vacation.StartDate);
        var to = vacations.Max(vacation => vacation.EndDate);

        return await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(request => request.EmployeeId == employeeId
                && request.Type == LeaveType.SickLeave
                && request.Status == LeaveRequestStatus.Approved
                && request.StartDate <= to
                && request.EndDate >= from)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> LoadNamesAsync(
        IEnumerable<Guid?> employeeIds,
        CancellationToken cancellationToken)
    {
        var ids = employeeIds.OfType<Guid>().Distinct().ToList();

        return await dbContext.Employees
            .Where(employee => ids.Contains(employee.Id))
            .ToDictionaryAsync(
                employee => employee.Id,
                employee => employee.FirstName + " " + employee.LastName,
                cancellationToken);
    }

    private static string? NameOf(Guid? employeeId, Dictionary<Guid, string> names) =>
        employeeId is { } id && names.TryGetValue(id, out var name) ? name : null;
}
