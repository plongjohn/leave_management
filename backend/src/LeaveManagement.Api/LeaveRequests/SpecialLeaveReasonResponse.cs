using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>
/// A special leave reason with its limit, so the request dialog can show it before any date is
/// chosen. The limits stay in the domain (ADR 0007); the client only displays them.
/// </summary>
public sealed record SpecialLeaveReasonResponse(
    SpecialLeaveReason Reason,
    decimal MaxWorkingDays,
    SpecialLeaveLimitPeriod Period)
{
    public static SpecialLeaveReasonResponse From(SpecialLeaveReason reason)
    {
        var limit = SpecialLeavePolicy.LimitFor(reason);
        return new SpecialLeaveReasonResponse(reason, limit.MaxWorkingDays, limit.Period);
    }
}
