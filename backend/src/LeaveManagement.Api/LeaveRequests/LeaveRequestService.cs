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

        var (leaveRequest, errorCode) = TrySubmit(submission, draft);

        // When only the certificate is missing, the employee still learns what it is for: the
        // vacation days that come back once it is attached. The error code stays.
        var sickLeave = errorCode == LeaveRequestErrors.MedicalCertificateRequired
            ? TrySubmit(submission, draft with { HasMedicalCertificate = true }).Request
            : leaveRequest;

        var needsApproval = draft.Type != LeaveType.SickLeave;

        return new LeaveRequestPreviewResponse(
            submission.Context.Calendar.CountWorkingDays(draft.Period),
            [.. submission.Holidays.Select(holiday => new HolidayResponse(holiday.Date, holiday.Name, holiday.Kind))],
            leaveRequest is { Type: LeaveType.Vacation } ? AvailableDaysAfter(submission, leaveRequest) : null,
            sickLeave is { Type: LeaveType.SickLeave } ? ReturnedVacationDays(submission, sickLeave) : 0m,
            errorCode,
            needsApproval,
            needsApproval ? await ManagerNameAsync(submission.Employee.ManagerId, cancellationToken) : null,
            await teamQueries.GetAbsentColleaguesAsync(employeeId, draft.Period, cancellationToken));
    }

    /// <returns>The id of the new request.</returns>
    public Task<Guid> SubmitAsync(
        Guid employeeId,
        LeaveRequestDraftRequest request,
        CancellationToken cancellationToken) =>
        SubmitLockedAsync(employeeId, request.ToDraft(), certificate: null, cancellationToken);

    /// <summary>Saves the sick leave and its certificate together, or neither.</summary>
    /// <returns>The id of the new request.</returns>
    public async Task<Guid> ReportSickLeaveAsync(
        Guid employeeId,
        ReportSickLeaveForm form,
        CancellationToken cancellationToken)
    {
        var certificate = form.Certificate is null ? null : await ReadCertificateAsync(form.Certificate, cancellationToken);
        return await SubmitLockedAsync(employeeId, form.ToDraftRequest().ToDraft(), certificate, cancellationToken);
    }

    private async Task<Guid> SubmitLockedAsync(
        Guid employeeId,
        LeaveRequestDraft draft,
        CertificateUpload? certificate,
        CancellationToken cancellationToken)
    {
        // Two requests submitted at the same time would both pass the overlap and balance checks.
        // Locking the employee's row serializes the submissions of one employee only.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM employees WHERE id = {employeeId} FOR UPDATE",
            cancellationToken);

        var submission = await LoadSubmissionAsync(employeeId, draft.Period, cancellationToken);
        var leaveRequest = LeaveRequest.Submit(submission.Employee, draft, submission.Context);

        dbContext.LeaveRequests.Add(leaveRequest);
        if (certificate is not null)
        {
            dbContext.MedicalCertificates.Add(MedicalCertificate.Create(
                leaveRequest, certificate.FileName, certificate.ContentType, certificate.Content, clock.Now()));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return leaveRequest.Id;
    }

    // The validator has checked size and type; the type is taken from the content, not from the client.
    private static async Task<CertificateUpload> ReadCertificateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream(capacity: (int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        var content = buffer.ToArray();

        var contentType = CertificateFileType.Detect(content)
            ?? throw new InvalidOperationException("The certificate was not validated.");

        return new CertificateUpload(file.FileName, contentType, content);
    }

    /// <param name="version">The version the employee saw; a change in the meantime becomes a 409.</param>
    /// <returns><c>false</c> if the request does not exist or belongs to someone else.</returns>
    public async Task<bool> CancelAsync(
        Guid employeeId,
        Guid requestId,
        uint version,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await dbContext.LeaveRequests
            .SingleOrDefaultAsync(request => request.Id == requestId && request.EmployeeId == employeeId, cancellationToken);
        if (leaveRequest is null)
        {
            return false;
        }

        // EF Core compares this value with xmin when saving: if the manager decided in the meantime,
        // the employee cancels something they have not seen.
        dbContext.Entry(leaveRequest).Property(request => request.Version).OriginalValue = version;

        leaveRequest.Cancel(clock.Today());
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
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
    // Expected in the preview: a violated rule is reported instead of failing the request.
    private static (LeaveRequest? Request, string? ErrorCode) TrySubmit(Submission submission, LeaveRequestDraft draft)
    {
        try
        {
            return (LeaveRequest.Submit(submission.Employee, draft, submission.Context), null);
        }
        catch (BusinessRuleViolationException violation)
        {
            return (null, violation.Code);
        }
    }

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

    private sealed record CertificateUpload(string FileName, string ContentType, byte[] Content);
}
