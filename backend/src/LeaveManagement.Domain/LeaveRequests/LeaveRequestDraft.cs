using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>What the employee asks for.</summary>
public sealed record LeaveRequestDraft(DateRange Period, LeaveType Type, string? Comment = null);
