using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>Error codes of the leave request rules. Clients map them to user-facing texts.</summary>
public static class LeaveRequestErrors
{
    public const string StartsInPast = "leave_request.starts_in_past";
    public const string StartsBeforeEntryDate = "leave_request.starts_before_entry_date";
    public const string NoWorkingDays = "leave_request.no_working_days";
    public const string Overlapping = "leave_request.overlapping";
    public const string NotPending = "leave_request.not_pending";
    public const string OwnRequest = "leave_request.own_request";
    public const string NotResponsibleManager = "leave_request.not_responsible_manager";
    public const string AlreadyStarted = "leave_request.already_started";
    public const string NotCancellable = "leave_request.not_cancellable";

    internal static BusinessRuleViolationException StartsInPastViolation() =>
        new(StartsInPast, "Only sick leave may start in the past.");

    internal static BusinessRuleViolationException StartsBeforeEntryDateViolation() =>
        new(StartsBeforeEntryDate, "The request starts before the employee's entry date.");

    internal static BusinessRuleViolationException NoWorkingDaysViolation() =>
        new(NoWorkingDays, "The requested period contains no working days.");

    internal static BusinessRuleViolationException OverlappingViolation() =>
        new(Overlapping, "The period overlaps with another pending or approved request.");

    internal static BusinessRuleViolationException NotPendingViolation(LeaveRequestStatus status) =>
        new(NotPending, $"Only pending requests can be decided; this one is {status}.");

    internal static BusinessRuleViolationException OwnRequestViolation() =>
        new(OwnRequest, "Nobody may decide on their own request.");

    internal static BusinessRuleViolationException NotResponsibleManagerViolation() =>
        new(NotResponsibleManager, "Only the employee's own manager may decide on the request.");

    internal static BusinessRuleViolationException AlreadyStartedViolation() =>
        new(AlreadyStarted, "Approved leave can only be cancelled before it starts.");

    internal static BusinessRuleViolationException NotCancellableViolation(LeaveRequestStatus status) =>
        new(NotCancellable, $"A {status} request cannot be cancelled.");
}
