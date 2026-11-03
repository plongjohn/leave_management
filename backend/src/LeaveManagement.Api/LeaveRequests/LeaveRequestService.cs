using LeaveManagement.Api.Common;
using LeaveManagement.Api.Holidays;
using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Holidays;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>
/// The use cases around the employee's own requests. Loads what the domain rules need
/// (<see cref="SubmissionContext"/>) and lets the domain decide.
/// </summary>
internal sealed class LeaveRequestService(
    LeaveManagementDbContext dbContext,
    CompanyClock clock,
    TeamQueries teamQueries)
{
    /// <summary>Runs the submission rules without saving anything.</summary>
    public async Task<LeaveRequestPreviewResponse> PreviewAsync(
        Guid employeeId,
        LeaveRequestDraftRequest request,
        CancellationToken cancellationToken)
    {
        var draft = request.ToDraft();
        var submission = await LoadSubmissionAsync(employeeId, draft.Period, cancellationToken);

        LeaveRequest? leaveRequest = null;
        string? errorCode = null;
        try
        {
            leaveRequest = LeaveRequest.Submit(submission.Employee, draft, submission.Context);
        }
        catch (BusinessRuleViolationException violation)
        {
            // Expected here: the preview reports the violated rule instead of failing.
            errorCode = violation.Code;
        }

        var needsApproval = draft.Type != LeaveType.SickLeave;

        return new LeaveRequestPreviewResponse(
            submission.Context.Calendar.CountWorkingDays(draft.Period),
            [.. submission.Holidays.Select(holiday => new HolidayResponse(holiday.Date, holiday.Name, holiday.Kind))],
            leaveRequest is { Type: LeaveType.Vacation } ? AvailableDaysAfter(submission, leaveRequest) : null,
            leaveRequest is { Type: LeaveType.SickLeave } ? ReturnedVacationDays(submission, leaveRequest) : 0m,
            errorCode,
            needsApproval,
            needsApproval ? await ManagerNameAsync(submission.Employee.ManagerId, cancellationToken) : null,
            await teamQueries.GetAbsentColleaguesAsync(employeeId, draft.Period, cancellationToken));
    }

    private async Task<Submission> LoadSubmissionAsync(
        Guid employeeId,
        DateRange period,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .SingleAsync(entry => entry.Id == employeeId, cancellationToken);

        var holidays = await dbContext.Holidays
            .AsNoTracking()
            .Where(holiday => holiday.Date >= period.Start && holiday.Date <= period.End)
            .OrderBy(holiday => holiday.Date)
            .ToListAsync(cancellationToken);

        // All active requests: the balance is allocated from the first day on, and care leave is
        // limited per working year (ADR 0007).
        var activeRequests = await dbContext.LeaveRequests
            .AsNoTracking()
            .Where(request => request.EmployeeId == employeeId
                && (request.Status == LeaveRequestStatus.Pending || request.Status == LeaveRequestStatus.Approved))
            .ToListAsync(cancellationToken);

        var entitlements = await dbContext.LeaveEntitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var context = new SubmissionContext(
            new WorkingDayCalendar(holidays),
            activeRequests,
            entitlements,
            clock.Today(),
            clock.Now());

        return new Submission(employee, context, holidays);
    }

    private static decimal AvailableDaysAfter(Submission submission, LeaveRequest newVacation)
    {
        var balance = LeaveBalanceCalculator.Calculate(
            submission.Context.Entitlements,
            submission.Context.ExistingRequests.Append(newVacation));

        return balance.AvailableIn(newVacation.StartDate.Year) - balance.UncoveredDays;
    }

    // Other sick leave cannot overlap the new one, so only the new sick leave returns these days.
    private static decimal ReturnedVacationDays(Submission submission, LeaveRequest newSickLeave) =>
        submission.Context.ExistingRequests
            .Where(request => request.Type == LeaveType.Vacation && request.Status == LeaveRequestStatus.Approved)
            .Sum(vacation => SickLeaveDuringVacation.ReturnedDays(vacation, [newSickLeave]));

    private async Task<string?> ManagerNameAsync(Guid? managerId, CancellationToken cancellationToken)
    {
        if (managerId is null)
        {
            return null;
        }

        return await dbContext.Employees
            .Where(employee => employee.Id == managerId)
            .Select(employee => employee.FirstName + " " + employee.LastName)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private sealed record Submission(Employee Employee, SubmissionContext Context, IReadOnlyList<Holiday> Holidays);
}
