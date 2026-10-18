using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>What the employee asks for.</summary>
/// <param name="SpecialLeaveReason">Required for special leave, not allowed for other types.</param>
public sealed record LeaveRequestDraft(
    DateRange Period,
    LeaveType Type,
    string? Comment = null,
    SpecialLeaveReason? SpecialLeaveReason = null);
