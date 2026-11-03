using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;

namespace LeaveManagement.Domain.LeaveRequests;

public sealed class LeaveRequest
{
    public const int CommentMaxLength = 500;

    private readonly List<LeaveRequestDay> _days = [];

    // Used by EF Core when materializing; parameter names match the property names.
    private LeaveRequest(
        Guid id,
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        LeaveType type,
        SpecialLeaveReason? specialLeaveReason,
        decimal workingDays,
        LeaveRequestStatus status,
        string? comment,
        bool hasMedicalCertificate,
        DateTimeOffset createdAt)
    {
        Id = id;
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
        Type = type;
        SpecialLeaveReason = specialLeaveReason;
        WorkingDays = workingDays;
        Status = status;
        Comment = comment;
        HasMedicalCertificate = hasMedicalCertificate;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public LeaveType Type { get; private set; }

    /// <summary>Set for special leave only.</summary>
    public SpecialLeaveReason? SpecialLeaveReason { get; private set; }

    public decimal WorkingDays { get; private set; }

    public LeaveRequestStatus Status { get; private set; }

    public string? Comment { get; private set; }

    /// <summary>Sick leave only: a medical certificate came with the report; the file is stored separately.</summary>
    public bool HasMedicalCertificate { get; private set; }

    /// <summary>Stored in UTC, like all timestamps.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? DecidedById { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>Concurrency token; mapped to PostgreSQL's <c>xmin</c> system column.</summary>
    public uint Version { get; private set; }

    public DateRange Period => new(StartDate, EndDate);

    /// <summary>
    /// The working days taken, calculated when the request is submitted. Stored as a snapshot so
    /// that later changes to the holiday calendar do not silently change existing requests.
    /// </summary>
    public IReadOnlyList<LeaveRequestDay> Days => _days;

    /// <summary>Pending requests reserve their days, approved ones take them.</summary>
    public bool IsActive => Status is LeaveRequestStatus.Pending or LeaveRequestStatus.Approved;

    public static LeaveRequest Submit(Employee employee, LeaveRequestDraft draft, SubmissionContext context)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(context);

        var period = draft.Period;
        var comment = NormalizeComment(draft.Comment);
        EnsureDetailsMatchType(draft);

        if (period.Start < employee.EntryDate)
        {
            throw LeaveRequestErrors.StartsBeforeEntryDateViolation();
        }

        if (draft.Type != LeaveType.SickLeave && period.Start < context.Today)
        {
            throw LeaveRequestErrors.StartsInPastViolation();
        }

        var days = period.Days()
            .Select(date => new LeaveRequestDay(date, context.Calendar.WorkingDaysOn(date)))
            .Where(day => day.WorkingDays > 0)
            .ToList();

        if (days.Count == 0)
        {
            throw LeaveRequestErrors.NoWorkingDaysViolation();
        }

        var activeRequests = context.ExistingRequests
            .Where(request => request.EmployeeId == employee.Id && request.IsActive)
            .ToList();

        EnsureNoOverlap(draft, activeRequests);

        var leaveRequest = new LeaveRequest(
            Guid.CreateVersion7(),
            employee.Id,
            period.Start,
            period.End,
            draft.Type,
            draft.SpecialLeaveReason,
            days.Sum(day => day.WorkingDays),
            InitialStatus(draft.Type),
            comment,
            draft.HasMedicalCertificate,
            context.Now.ToUniversalTime());
        leaveRequest._days.AddRange(days);

        if (leaveRequest.Type == LeaveType.Vacation)
        {
            EnsureEntitlementCovers(leaveRequest, activeRequests, context.Entitlements);
        }

        if (leaveRequest.Type == LeaveType.SpecialLeave)
        {
            SpecialLeavePolicy.EnsureWithinLimits(leaveRequest, employee, activeRequests);
        }

        return leaveRequest;
    }

    public void Approve(Employee approver, Employee requester, DateTimeOffset now)
    {
        EnsureCanBeDecidedBy(approver, requester);
        Decide(LeaveRequestStatus.Approved, approver, now);
    }

    public void Reject(Employee approver, Employee requester, DateTimeOffset now)
    {
        EnsureCanBeDecidedBy(approver, requester);
        Decide(LeaveRequestStatus.Rejected, approver, now);
    }

    /// <param name="today">The current date in the company's time zone.</param>
    public void Cancel(DateOnly today)
    {
        switch (Status)
        {
            case LeaveRequestStatus.Pending:
                break;
            case LeaveRequestStatus.Approved when today < StartDate:
                break;
            case LeaveRequestStatus.Approved:
                throw LeaveRequestErrors.AlreadyStartedViolation();
            default:
                throw LeaveRequestErrors.NotCancellableViolation(Status);
        }

        Status = LeaveRequestStatus.Cancelled;
    }

    private static LeaveRequestStatus InitialStatus(LeaveType type) =>
        type == LeaveType.SickLeave ? LeaveRequestStatus.Approved : LeaveRequestStatus.Pending;

    private static void EnsureDetailsMatchType(LeaveRequestDraft draft)
    {
        if (draft.Type == LeaveType.SpecialLeave && draft.SpecialLeaveReason is null)
        {
            throw LeaveRequestErrors.SpecialLeaveReasonRequiredViolation();
        }

        if (draft.Type != LeaveType.SpecialLeave && draft.SpecialLeaveReason is not null)
        {
            throw LeaveRequestErrors.SpecialLeaveReasonNotAllowedViolation();
        }

        if (draft.SpecialLeaveReason is { } reason && !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(draft), "Unknown special leave reason.");
        }

        if (draft.Type != LeaveType.SickLeave && draft.HasMedicalCertificate)
        {
            throw new ArgumentException("Only sick leave comes with a medical certificate.", nameof(draft));
        }
    }

    // Sick leave may lie in approved vacation: the employee falls ill, the vacation days come back
    // (SickLeaveDuringVacation). Any other overlap means the same day would be requested twice.
    private static void EnsureNoOverlap(LeaveRequestDraft draft, IEnumerable<LeaveRequest> activeRequests)
    {
        var overlapping = activeRequests.Where(request => request.Period.Overlaps(draft.Period)).ToList();
        if (overlapping.Count == 0)
        {
            return;
        }

        if (draft.Type != LeaveType.SickLeave)
        {
            throw LeaveRequestErrors.OverlappingViolation();
        }

        // A pending vacation could still be approved for days the employee was ill.
        if (overlapping.Any(request => request.Type == LeaveType.Vacation && request.Status == LeaveRequestStatus.Pending))
        {
            throw LeaveRequestErrors.OverlapsPendingVacationViolation();
        }

        if (!overlapping.All(SickLeaveDuringVacation.MayOverlap))
        {
            throw LeaveRequestErrors.OverlappingViolation();
        }

        if (SickLeaveDuringVacation.RequiresCertificate(draft.Period) && !draft.HasMedicalCertificate)
        {
            throw LeaveRequestErrors.MedicalCertificateRequiredViolation();
        }
    }

    // The whole allocation is recalculated: an earlier new request can take days that a later,
    // already planned request relied on, so every request must still be covered afterwards.
    private static void EnsureEntitlementCovers(
        LeaveRequest newRequest,
        IEnumerable<LeaveRequest> activeRequests,
        IEnumerable<LeaveEntitlement> entitlements)
    {
        var balance = LeaveBalanceCalculator.Calculate(
            entitlements.Where(entitlement => entitlement.EmployeeId == newRequest.EmployeeId),
            activeRequests.Append(newRequest));

        if (!balance.IsCovered)
        {
            throw LeaveRequestErrors.InsufficientBalanceViolation(balance.UncoveredDays);
        }
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var trimmed = comment.Trim();
        if (trimmed.Length > CommentMaxLength)
        {
            throw new ArgumentException($"Must not be longer than {CommentMaxLength} characters.", nameof(comment));
        }

        return trimmed;
    }

    // The own manager decides. HR decides only for employees without a manager (e.g. the
    // managing director), so every request has a responsible party and HR cannot bypass a manager.
    private void EnsureCanBeDecidedBy(Employee approver, Employee requester)
    {
        ArgumentNullException.ThrowIfNull(approver);
        ArgumentNullException.ThrowIfNull(requester);

        if (requester.Id != EmployeeId)
        {
            throw new ArgumentException("Must be the employee who submitted the request.", nameof(requester));
        }

        if (Status != LeaveRequestStatus.Pending)
        {
            throw LeaveRequestErrors.NotPendingViolation(Status);
        }

        if (approver.Id == EmployeeId)
        {
            throw LeaveRequestErrors.OwnRequestViolation();
        }

        var isResponsible = requester.ManagerId is { } managerId
            ? approver.Id == managerId
            : approver.Role == EmployeeRole.Hr;

        if (!isResponsible)
        {
            throw LeaveRequestErrors.NotResponsibleApproverViolation();
        }
    }

    private void Decide(LeaveRequestStatus status, Employee approver, DateTimeOffset now)
    {
        Status = status;
        DecidedById = approver.Id;
        DecidedAt = now.ToUniversalTime();
    }
}
