using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>What the employee asks for.</summary>
/// <param name="SpecialLeaveReason">Required for special leave, not allowed for other types.</param>
/// <param name="HasMedicalCertificate">
/// Sick leave only: a certificate comes with the report. Required for longer sick leave during
/// approved vacation (<see cref="SickLeaveDuringVacation"/>).
/// </param>
public sealed record LeaveRequestDraft(
    DateRange Period,
    LeaveType Type,
    string? Comment = null,
    SpecialLeaveReason? SpecialLeaveReason = null,
    bool HasMedicalCertificate = false);
