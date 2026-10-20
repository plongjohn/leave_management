using System.Globalization;
using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>Error codes of the leave request rules. Clients map them to user-facing texts.</summary>
public static class LeaveRequestErrors
{
    public const string StartsInPast = "leave_request.starts_in_past";
    public const string StartsBeforeEntryDate = "leave_request.starts_before_entry_date";
    public const string NoWorkingDays = "leave_request.no_working_days";
    public const string Overlapping = "leave_request.overlapping";
    public const string InsufficientBalance = "leave_request.insufficient_balance";
    public const string SpecialLeaveReasonRequired = "leave_request.special_leave_reason_required";
    public const string SpecialLeaveReasonNotAllowed = "leave_request.special_leave_reason_not_allowed";
    public const string SpecialLeaveLimitExceeded = "leave_request.special_leave_limit_exceeded";
    public const string NotPending = "leave_request.not_pending";
    public const string OwnRequest = "leave_request.own_request";
    public const string NotResponsibleApprover = "leave_request.not_responsible_approver";
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

    internal static BusinessRuleViolationException InsufficientBalanceViolation(decimal uncoveredDays) =>
        new(
            InsufficientBalance,
            string.Create(
                CultureInfo.InvariantCulture,
                $"The leave entitlement does not cover {uncoveredDays} of the requested or already planned working days."));

    internal static BusinessRuleViolationException SpecialLeaveReasonRequiredViolation() =>
        new(SpecialLeaveReasonRequired, "Special leave needs a reason.");

    internal static BusinessRuleViolationException SpecialLeaveReasonNotAllowedViolation() =>
        new(SpecialLeaveReasonNotAllowed, "Only special leave has a reason.");

    internal static BusinessRuleViolationException SpecialLeaveLimitExceededViolation(
        SpecialLeaveReason reason,
        decimal maxDays) =>
        new(
            SpecialLeaveLimitExceeded,
            string.Create(CultureInfo.InvariantCulture, $"Special leave for {reason} is limited to {maxDays} working days."));

    internal static BusinessRuleViolationException NotPendingViolation(LeaveRequestStatus status) =>
        new(NotPending, $"Only pending requests can be decided; this one is {status}.");

    internal static BusinessRuleViolationException OwnRequestViolation() =>
        new(OwnRequest, "Nobody may decide on their own request.");

    internal static BusinessRuleViolationException NotResponsibleApproverViolation() =>
        new(
            NotResponsibleApprover,
            "Only the employee's own manager may decide on the request; HR decides for employees without a manager.");

    internal static BusinessRuleViolationException AlreadyStartedViolation() =>
        new(AlreadyStarted, "Approved leave can only be cancelled before it starts.");

    internal static BusinessRuleViolationException NotCancellableViolation(LeaveRequestStatus status) =>
        new(NotCancellable, $"A {status} request cannot be cancelled.");
}
